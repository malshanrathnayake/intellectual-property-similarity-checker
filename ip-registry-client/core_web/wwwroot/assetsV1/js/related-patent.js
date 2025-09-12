document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("uploadForm");
    const spinner = document.getElementById("loadingSpinner");
    const extracted = document.getElementById("extractedContent");
    const errorBox = document.getElementById("errorMessage");
    const rejectionBox = document.getElementById("rejectionDetails");
    const rejectionList = document.getElementById("similarResultsList");

    loadApprovedPatents();

    form.addEventListener("submit", async function (e) {
        e.preventDefault();

        const fileInput = document.getElementById("pdfFile");
        const file = fileInput.files[0];
        if (!file) return;

        const formData = new FormData();
        formData.append("file", file);

        // Reset states
        spinner.classList.remove("d-none");
        extracted.classList.add("d-none");
        errorBox.classList.add("d-none");
        rejectionBox.classList.add("d-none");
        rejectionList.innerHTML = "";

        try {
            const response = await fetch("http://localhost:8000/register/pdf", {
                method: "POST",
                body: formData,
            });

            const data = await response.json();
            spinner.classList.add("d-none");

            if (data.success && data.extracted) {
                document.getElementById("titleText").innerText = data.extracted.title || "N/A";

                const abstract = data.extracted.abstract || "N/A";
                const claims = Array.isArray(data.extracted.claims)
                    ? data.extracted.claims.join(" ")
                    : data.extracted.claims || "N/A";

                const abstractHtml = renderToggleText(abstract, "dynamicAbstract");
                const claimsHtml = renderToggleText(claims, "dynamicClaims");

                document.getElementById("abstractToggleContainer").innerHTML = abstractHtml;
                document.getElementById("claimsToggleContainer").innerHTML = claimsHtml;

                extracted.classList.remove("d-none");

                showToast("Patent approved and stored on IPFS + Blockchain", "success");

                rejectionBox.classList.add("d-none");
                rejectionList.innerHTML = "";

                loadApprovedPatents();
            } else if (!data.success && data.status === "rejected" && data.similar) {
                rejectionBox.classList.remove("d-none");

                data.similar.forEach((item, index) => {
                    const collapseId = `collapse${index}`;
                    const html = `
                        <div class="card mb-2">
                            <div class="card-header d-flex justify-content-between align-items-center">
                                <span><strong>ID:</strong> ${item.id}</span>
                                <button class="btn btn-sm btn-outline-secondary" data-bs-toggle="collapse" data-bs-target="#${collapseId}">
                                    View Details
                                </button>
                            </div>
                            <div id="${collapseId}" class="collapse">
                                <div class="card-body">
                                    <p><strong>Title:</strong> ${item.title}</p>
                                    <p><strong>Distance:</strong> ${item.faiss_distance}</p>
                                    ${item.pdf_cid ? `
                                        <a href="https://gateway.pinata.cloud/ipfs/${item.pdf_cid}" target="_blank" class="btn btn-sm btn-outline-primary me-2">View</a>
                                        <a href="https://gateway.pinata.cloud/ipfs/${item.pdf_cid}?download=true" target="_blank" class="btn btn-sm btn-outline-success">Download</a>
                                    ` : item.cid ? `
                                        <a href="https://gateway.pinata.cloud/ipfs/${item.cid}" target="_blank" class="btn btn-sm btn-outline-info">View Metadata</a>
                                    ` : `
                                        <span class="text-muted">No CID available</span>
                                    `}

                                </div>
                            </div>
                        </div>
                    `;
                    rejectionList.insertAdjacentHTML("beforeend", html);
                });
            } else {
                throw new Error(data.message || "Extraction failed.");
            }
        } catch (err) {
            spinner.classList.add("d-none");
            errorBox.innerText = err.message;
            errorBox.classList.remove("d-none");
        }
    });

    function renderToggleText(fullText, containerId, threshold = 300) {
        const shortText = fullText.length > threshold ? fullText.slice(0, threshold) + "..." : fullText;
        const uid = `${containerId}-${Date.now()}-${Math.floor(Math.random() * 1000)}`;

        if (fullText.length <= threshold) return `<p>${fullText}</p>`;

        return `
            <p id="${uid}-short">${shortText}
                <a href="#" onclick="toggleText('${uid}', true); return false;" class="ms-2">Show More</a>
            </p>
            <p id="${uid}-full" style="display: none;">
                ${fullText}
                <a href="#" onclick="toggleText('${uid}', false); return false;" class="ms-2">Show Less</a>
            </p>
        `;
    }

    window.toggleText = function (uid, showFull) {
        document.getElementById(`${uid}-short`).style.display = showFull ? "none" : "block";
        document.getElementById(`${uid}-full`).style.display = showFull ? "block" : "none";
    };

    function showToast(message, type = "info") {
        const toastId = `toast-${Date.now()}`;
        const toastColor = type === "success" ? "bg-success" : "bg-danger";

        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center text-white ${toastColor} border-0 mb-2" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">${message}</div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;

        const toastContainer = document.getElementById("toastContainer");
        toastContainer.insertAdjacentHTML("beforeend", toastHtml);

        new bootstrap.Toast(document.getElementById(toastId), { delay: 5000 }).show();
    }

    async function loadApprovedPatents() {
        try {
            const res = await fetch("http://localhost:8000/registered");
            const data = await res.json();

            const container = document.getElementById("approvedPatentsList");
            container.innerHTML = "";

            data.patents.forEach((item, index) => {
                const abstractHtml = renderToggleText(item.abstract, `approvedAbstract${index}`);
                const claimsHtml = renderToggleText(
                    Array.isArray(item.claims) ? item.claims.join(" ") : item.claims,
                    `approvedClaims${index}`
                );

                const html = `
                <div class="card mb-3">
                    <div class="card-header d-flex justify-content-between align-items-center">
                        <span><strong>ID:</strong> ${item.id}</span>
                        <span><strong>Title:</strong> ${item.title}</span>
                    </div>
                    <div class="card-body">
                        <p><strong>Abstract:</strong><br>${abstractHtml}</p>
                        <p><strong>Claims:</strong><br>${claimsHtml}</p>
                        ${item.pdf_cid ? `
                            <a href="https://gateway.pinata.cloud/ipfs/${item.pdf_cid}" target="_blank" class="btn btn-sm btn-outline-primary me-2">View</a>
                            <a href="https://gateway.pinata.cloud/ipfs/${item.pdf_cid}?download=true" class="btn btn-sm btn-outline-success" download>Download</a>
                        ` : `
                            <a href="https://gateway.pinata.cloud/ipfs/${item.cid}" target="_blank" class="btn btn-sm btn-outline-info">View Metadata</a>
                        `}
                    </div>
                </div>
                `;
                container.insertAdjacentHTML("beforeend", html);
            });
        } catch (err) {
            console.error("❌ Error loading registered patents:", err);
        }
    }
});
