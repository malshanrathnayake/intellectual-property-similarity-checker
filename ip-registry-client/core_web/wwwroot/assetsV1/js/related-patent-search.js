document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("searchForm");
    const queryInput = document.getElementById("queryInput");
    const resultsBox = document.getElementById("searchResults");
    const resultsContainer = document.getElementById("resultsContainer");
    const errorBox = document.getElementById("searchError");

    form.addEventListener("submit", async function (e) {
        e.preventDefault();

        const query = queryInput.value.trim();
        if (!query) return;

        resultsBox.classList.add("d-none");
        errorBox.classList.add("d-none");
        resultsContainer.innerHTML = "";

        try {
            const response = await fetch(`http://localhost:8000/search?query=${encodeURIComponent(query)}`);
            const data = await response.json();

            if (data.results_found === 0) {
                resultsContainer.innerHTML = `<p>No semantically similar patents found.</p>`;
            } else {
                data.results.forEach((item, index) => {
                    const hasCid = !!item.cid;
                    const abstractShort = item.abstract ? item.abstract.slice(0, 250) + "..." : "N/A";
                    const claimsShort = Array.isArray(item.claims)
                        ? item.claims.slice(0, 2).join(" ") + "..."
                        : item.claims?.slice(0, 250) + "...";

                    const html = `
                        <div class="card mb-3">
                            <div class="card-header d-flex justify-content-between align-items-center">
                                <span><strong>ID:</strong> ${item.id}</span>
                                <span><strong>Distance:</strong> ${item.faiss_distance}</span>
                            </div>
                            <div class="card-body">
                                <p><strong>Title:</strong> ${item.title}</p>
                                <p><strong>Abstract:</strong> ${abstractShort}</p>
                                <p><strong>Claims:</strong> ${claimsShort}</p>
                                ${hasCid ? `
                                    <a href="https://gateway.pinata.cloud/ipfs/${item.cid}" target="_blank" class="btn btn-sm btn-outline-primary me-2">View</a>
                                    <a href="https://gateway.pinata.cloud/ipfs/${item.cid}?download=true" target="_blank" class="btn btn-sm btn-outline-success">Download</a>
                                ` : `<span class="text-muted">No CID available</span>`}
                            </div>
                        </div>
                    `;
                    resultsContainer.insertAdjacentHTML("beforeend", html);
                });
            }

            resultsBox.classList.remove("d-none");
        } catch (err) {
            console.error(err);
            errorBox.innerText = "An error occurred while searching. Please try again.";
            errorBox.classList.remove("d-none");
        }
    });
});
