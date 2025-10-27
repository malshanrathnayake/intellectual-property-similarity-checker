document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("searchForm");
    const queryInput = document.getElementById("queryInput");
    const resultsBox = document.getElementById("searchResults");
    const resultsContainer = document.getElementById("resultsContainer");
    const errorBox = document.getElementById("searchError");
    const chartCard = document.getElementById("chartCard");
    const spinner = document.getElementById('loadingSpinner');


    form.addEventListener("submit", async function (e) {
        e.preventDefault();

        const query = queryInput.value.trim();
        if (!query) return;

        // Hide results and chart while loading
        resultsBox.classList.add("d-none");
        errorBox.classList.add("d-none");
        chartCard.classList.add("d-none");
        resultsContainer.innerHTML = "";
        spinner.classList.remove("d-none");

        try {
            const response = await fetch(`https://related-patent-discovery.azurewebsites.net/search?query=${encodeURIComponent(query)}`);
            const data = await response.json();
            spinner.classList.add("d-none");

            if (data.results_found === 0) {
                resultsContainer.innerHTML = `<p>No semantically similar patents found.</p>`;
                chartCard.classList.add("d-none");
            } else {
                // Render results
                data.results.forEach((item, index) => {
                    const hasCid = !!item.cid;
                    const abstractHtml = renderToggleText(item.abstract || "N/A", `abstract${index}`);
                    const claimsFull = Array.isArray(item.claims) ? item.claims.join(" ") : item.claims || "N/A";
                    const claimsHtml = renderToggleText(claimsFull, `claims${index}`);

                    const card = document.createElement("div");
                    card.className = "card mb-3";
                    card.innerHTML = `
                        <div class="card-header d-flex justify-content-between align-items-center">
                            <span><strong>ID:</strong> ${item.id}</span>
                            <span><strong>Similarity:</strong> ${item.similarity_percent?.toFixed(2) || ((1 - item.faiss_distance) * 100).toFixed(2)}%</span>
                        </div>
                        <div class="card-body">
                            <p><strong>Title:</strong> ${item.title}</p>
                            <p><strong>Abstract:</strong></p>
                            ${abstractHtml}
                            <p><strong>Claims:</strong></p>
                            ${claimsHtml}
                            ${hasCid ? `
                                <a href="https://gateway.pinata.cloud/ipfs/${item.cid}" target="_blank" class="btn btn-sm btn-outline-primary me-2">View</a>
                                <a href="https://gateway.pinata.cloud/ipfs/${item.cid}?download=true" target="_blank" class="btn btn-sm btn-outline-success">Download</a>
                            ` : `<span class="text-muted">No CID available</span>`}
                        </div>
`                   ;
                    resultsContainer.appendChild(card);

                });

                try {
                    drawChart(data.results);
                    chartCard.classList.remove("d-none");
                } catch (chartError) {
                    console.error("Chart rendering failed:", chartError);
                    errorBox.innerText = "Search succeeded, but failed to render similarity chart.";
                    errorBox.classList.remove("d-none");
                }

            }

            resultsBox.classList.remove("d-none");
        } catch (err) {
            console.error(err);
            spinner.classList.add("d-none");
            errorBox.innerText = "An error occurred while searching. Please try again.";
            errorBox.classList.remove("d-none");
        }
    });
});

function renderToggleText(content, id, charLimit = 250) {
    if (!content || content.length <= charLimit) {
        return `<p>${content}</p>`;
    }

    const shortText = content.slice(0, charLimit);
    const fullText = content;

    return `
        <p id="${id}-short">${shortText}... <a href="#" onclick="toggleText('${id}', true); return false;">Show More</a></p>
        <p id="${id}-full" style="display: none;">${fullText} <a href="#" onclick="toggleText('${id}', false); return false;">Show Less</a></p>
    `;
}

function toggleText(id, showFull) {
    const shortEl = document.getElementById(`${id}-short`);
    const fullEl = document.getElementById(`${id}-full`);
    if (showFull) {
        shortEl.style.display = "none";
        fullEl.style.display = "block";
    } else {
        shortEl.style.display = "block";
        fullEl.style.display = "none";
    }
}

//function drawChart(data) {
//    const svg = d3.select("#similarityChart");
//    svg.selectAll("*").remove(); // Clear previous chart

//    const width = svg.node().getBoundingClientRect().width || 600; // fallback if 0
//    const height = +svg.attr("height") - 40;
//    const margin = { top: 20, right: 20, bottom: 20, left: 250 };

//    // Scale for X axis (distance)
//    const x = d3.scaleLinear()
//        .domain([0, 1])
//        .range([margin.left, width - margin.right]);

//    // Scale for Y axis (title)
//    const y = d3.scaleBand()
//        .domain(data.map(d => d.title.length > 40 ? d.title.slice(0, 40) + "..." : d.title))
//        .range([margin.top, height])
//        .padding(0.2);

//    // Color scale: green → yellow → red
//    const colorScale = d3.scaleLinear()
//        .domain([0.0, 0.5, 1.0])
//        .range(["#28a745", "#ffc107", "#dc3545"]);

//    // X-axis
//    svg.append("g")
//        .attr("transform", `translate(0,${height + margin.top})`)
//        .call(d3.axisBottom(x).ticks(5));

//    // Y-axis
//    svg.append("g")
//        .attr("transform", `translate(${margin.left},0)`)
//        .call(d3.axisLeft(y));

//    // Tooltip div
//    const tooltip = d3.select("body").append("div")
//        .attr("class", "tooltip")
//        .style("position", "absolute")
//        .style("padding", "6px 12px")
//        .style("background", "#333")
//        .style("color", "#fff")
//        .style("border-radius", "4px")
//        .style("font-size", "13px")
//        .style("pointer-events", "none")
//        .style("opacity", 0);

//    // Bars
//    svg.selectAll(".bar")
//        .data(data)
//        .enter()
//        .append("rect")
//        .attr("x", x(0))
//        .attr("y", d => y(d.title.length > 40 ? d.title.slice(0, 40) + "..." : d.title))
//        .attr("width", d => x(d.faiss_distance) - x(0))
//        .attr("height", y.bandwidth())
//        .attr("fill", d => colorScale(d.faiss_distance))
//        .on("mouseover", function (event, d) {
//            tooltip.transition().duration(100).style("opacity", 1);
//            tooltip.html(`<strong>${d.title}</strong><br/>Distance: ${d.faiss_distance.toFixed(4)}`);
//        })
//        .on("mousemove", function (event) {
//            tooltip.style("left", (event.pageX + 10) + "px")
//                .style("top", (event.pageY - 28) + "px");
//        })
//        .on("mouseout", function () {
//            tooltip.transition().duration(200).style("opacity", 0);
//        });

//    // Rounded markers at bar ends
//    svg.selectAll(".end-circle")
//        .data(data)
//        .enter()
//        .append("circle")
//        .attr("cx", d => x(d.faiss_distance))
//        .attr("cy", d => y(d.title.length > 40 ? d.title.slice(0, 40) + "..." : d.title) + y.bandwidth() / 2)
//        .attr("r", 5)
//        .attr("fill", "#ffffff")
//        .attr("stroke", d => colorScale(d.faiss_distance))
//        .attr("stroke-width", 2);

//    // Value Labels
//    svg.selectAll(".label")
//        .data(data)
//        .enter()
//        .append("text")
//        .attr("x", d => x(d.faiss_distance) + 10)
//        .attr("y", d => y(d.title.length > 40 ? d.title.slice(0, 40) + "..." : d.title) + y.bandwidth() / 1.5)
//        .text(d => d.faiss_distance.toFixed(4))
//        .attr("fill", "#ffffff")
//        .style("font-size", "12px");
//}

function drawChart(data) {
    const svg = d3.select("#similarityChart");
    svg.selectAll("*").remove(); // Clear previous chart

    // --- Convert FAISS distance → similarity %
    data.forEach(d => {
        d.similarity_percent = Math.max(0, (1 - Math.min(d.faiss_distance, 1)) * 100);
    });

    const width = svg.node().getBoundingClientRect().width || 600;
    const height = +svg.attr("height") - 40;
    const margin = { top: 20, right: 20, bottom: 20, left: 250 };

    // X scale for similarity %
    const x = d3.scaleLinear()
        .domain([0, 100]) // now 0–100%
        .range([margin.left, width - margin.right]);

    // Y scale for titles
    const y = d3.scaleBand()
        .domain(data.map(d => d.title.length > 40 ? d.title.slice(0, 40) + "..." : d.title))
        .range([margin.top, height])
        .padding(0.2);

    // Color: red → yellow → green (low → high similarity)
    const colorScale = d3.scaleLinear()
        .domain([0, 50, 100])
        .range(["#dc3545", "#ffc107", "#28a745"]); // red → yellow → green

    // X-axis
    svg.append("g")
        .attr("transform", `translate(0,${height + margin.top})`)
        .call(d3.axisBottom(x).ticks(5).tickFormat(d => d + "%"));

    // Y-axis
    svg.append("g")
        .attr("transform", `translate(${margin.left},0)`)
        .call(d3.axisLeft(y));

    // Tooltip
    const tooltip = d3.select("body").append("div")
        .attr("class", "tooltip")
        .style("position", "absolute")
        .style("padding", "6px 12px")
        .style("background", "#333")
        .style("color", "#fff")
        .style("border-radius", "4px")
        .style("font-size", "13px")
        .style("pointer-events", "none")
        .style("opacity", 0);

    // Bars (width based on similarity %)
    svg.selectAll(".bar")
        .data(data)
        .enter()
        .append("rect")
        .attr("x", x(0))
        .attr("y", d => y(d.title.length > 40 ? d.title.slice(0, 40) + "..." : d.title))
        .attr("width", d => x(d.similarity_percent) - x(0))
        .attr("height", y.bandwidth())
        .attr("fill", d => colorScale(d.similarity_percent))
        .on("mouseover", function (event, d) {
            tooltip.transition().duration(100).style("opacity", 1);
            tooltip.html(
                `<strong>${d.title}</strong><br/>
                 Similarity: ${d.similarity_percent.toFixed(2)}%<br/>
                 Distance: ${d.faiss_distance.toFixed(4)}`
            );
        })
        .on("mousemove", event => {
            tooltip.style("left", (event.pageX + 10) + "px")
                .style("top", (event.pageY - 28) + "px");
        })
        .on("mouseout", () => tooltip.transition().duration(200).style("opacity", 0));

    // Circles at the end of bars
    svg.selectAll(".end-circle")
        .data(data)
        .enter()
        .append("circle")
        .attr("cx", d => x(d.similarity_percent))
        .attr("cy", d => y(d.title.length > 40 ? d.title.slice(0, 40) + "..." : d.title) + y.bandwidth() / 2)
        .attr("r", 5)
        .attr("fill", "#fff")
        .attr("stroke", d => colorScale(d.similarity_percent))
        .attr("stroke-width", 2);

    // Labels
    svg.selectAll(".label")
        .data(data)
        .enter()
        .append("text")
        .attr("x", d => x(d.similarity_percent) + 10)
        .attr("y", d => y(d.title.length > 40 ? d.title.slice(0, 40) + "..." : d.title) + y.bandwidth() / 1.5)
        .text(d => `${d.similarity_percent.toFixed(1)}%`)
        .attr("fill", "#fff")
        .style("font-size", "12px");
}


