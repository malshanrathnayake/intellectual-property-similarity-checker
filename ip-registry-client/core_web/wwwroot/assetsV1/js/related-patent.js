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

            if (data.success && data.extracted) {
                // ✅ Show extracted content
                document.getElementById("titleText").innerText = data.extracted.title || "N/A";
                document.getElementById("abstractTextShort").innerText = data.extracted.abstract.slice(0, 300) + "...";
                document.getElementById("abstractTextFull").innerText = data.extracted.abstract;
                let fullClaims = data.extracted.claims || "N/A";

                // If it's an array, join it into a single string
                if (Array.isArray(fullClaims)) {
                    fullClaims = fullClaims.join(" ");
                }

                // Proceed with intelligent truncation
                const claimSentences = fullClaims.split(/[.?!]\s+/);
                const shortClaims = claimSentences.slice(0, 2).join('. ') + '...';

                document.getElementById("claimsTextShort").innerText = shortClaims;
                document.getElementById("claimsTextFull").innerText = fullClaims;


                extracted.classList.remove("d-none");

                // ✅ Toast notification
                showToast("Patent approved and stored on IPFS + Blockchain", "success");
                loadApprovedPatents();
            } else if (data.status === "rejected" && data.similar) {
                // ❌ Show rejected reason and similar results
                rejectionBox.classList.remove("d-none");

                data.similar.forEach((item, index) => {
                    const collapseId = `collapse${index}`;
                    const hasCid = !!item.cid;
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
                                    ${hasCid ? `
                                        <a href="https://gateway.pinata.cloud/ipfs/${item.cid}" target="_blank" class="btn btn-sm btn-outline-primary me-2">View</a>
                                        <a href="https://gateway.pinata.cloud/ipfs/${item.cid}?download=true" target="_blank" class="btn btn-sm btn-outline-success">Download</a>
                                    ` : `<span class="text-muted">No CID available</span>`}
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
            errorBox.innerText = err.message;
            errorBox.classList.remove("d-none");
        } finally {
            spinner.classList.add("d-none");
        }
    });

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

        const toastElement = new bootstrap.Toast(document.getElementById(toastId), { delay: 5000 });
        toastElement.show();
    }

    async function loadApprovedPatents() {
        try {
            const res = await fetch("http://localhost:8000/registered");
            const data = await res.json();

            const container = document.getElementById("approvedPatentsList");
            container.innerHTML = "";

            data.patents.forEach((item, index) => {
                const shortAbstract = item.abstract.slice(0, 250) + "...";
                const shortClaims = Array.isArray(item.claims)
                    ? item.claims.slice(0, 2).join(" ") + "..."
                    : item.claims.slice(0, 250) + "...";

                const html = `
                <div class="card mb-3">
                    <div class="card-header d-flex justify-content-between align-items-center">
                        <span><strong>ID:</strong> ${item.id}</span>
                        <span><strong>Title:</strong> ${item.title}</span>
                    </div>
                    <div class="card-body">
                        <p><strong>Abstract:</strong> ${shortAbstract}</p>
                        <p><strong>Claims:</strong> ${shortClaims}</p>
                        <a href="https://gateway.pinata.cloud/ipfs/${item.cid}" target="_blank" class="btn btn-sm btn-outline-primary me-2">View</a>
                        <a href="https://gateway.pinata.cloud/ipfs/${item.cid}?download=true" target="_blank" class="btn btn-sm btn-outline-success">Download</a>
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
