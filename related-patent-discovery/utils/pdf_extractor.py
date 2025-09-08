import fitz  # PyMuPDF
import pytesseract
from pdf2image import convert_from_bytes
import re
from io import BytesIO

# Set Tesseract path manually (for Windows)
pytesseract.pytesseract.tesseract_cmd = r"C:\Program Files\Tesseract-OCR\tesseract.exe"

# ---------------------------------------------------------
# Extract text using PyMuPDF
# ---------------------------------------------------------
def extract_text_fitz(file_bytes: bytes) -> str:
    try:
        doc = fitz.open(stream=file_bytes, filetype="pdf")
        return "\n".join(page.get_text() for page in doc)
    except Exception as e:
        print("Text extraction with fitz failed:", e)
        return ""

# ---------------------------------------------------------
# Extract text using OCR fallback (pdf2image + pytesseract)
# ---------------------------------------------------------
# def extract_text_ocr(file_bytes: bytes, max_pages=10) -> str:
#     try:
#         images = convert_from_bytes(file_bytes, dpi=200)[:max_pages]
#         return "\n".join(pytesseract.image_to_string(img) for img in images)
#     except Exception as e:
#         print("OCR fallback failed:", e)
#         return ""
def extract_text_ocr(file_bytes: bytes, max_pages=5) -> str:
    try:
        images = convert_from_bytes(file_bytes, dpi=200)[:max_pages]
        ocr_result = []
        for i, img in enumerate(images):
            text = pytesseract.image_to_string(img)
            if len(text.strip()) > 100:
                ocr_result.append(text.strip())
            # Stop early if good text found from first 3 pages
            if i >= 2 and len(" ".join(ocr_result)) > 300:
                break
        return "\n".join(ocr_result)
    except Exception as e:
        print("OCR fallback failed:", e)
        return ""

# ---------------------------------------------------------
# Main utility: Extract title, abstract, and claims
# ---------------------------------------------------------
def extract_patent_sections(file_bytes: bytes) -> dict:
    fitz_text = extract_text_fitz(file_bytes).strip()
    source = "PDF"

    # Heuristic to decide fallback — weak text detection
    if len(fitz_text) < 300 or fitz_text.count("\n") < 5:
        print("[INFO] Detected weak text, running OCR fallback...")
        ocr_text = extract_text_ocr(file_bytes)
        full_text = f"{fitz_text}\n\n{ocr_text}".strip()
        source = "OCR"
    else:
        full_text = fitz_text

    lines = [line.strip() for line in full_text.splitlines() if line.strip()]
    lowered = "\n".join(lines).lower()

    # --- Title ---
    title = next((line for line in lines if "patent" in line.lower() and len(line) < 100), "Untitled")

    # --- Abstract ---
    # --- Optimized Abstract Extraction ---
    abstract_match = re.search(
        r"(?i)(?<=abstract)(?:\s*[:\-\.]?\s*)(.+?)(?=\n\s*(?:field of invention|technical field|field|background|summary|claims|description|brief description|detailed description|\n\d+\s*\.))",
        full_text, re.DOTALL
    )

    abstract = ""
    if abstract_match:
        abstract_candidate = abstract_match.group(1).strip()
        abstract_lines = abstract_candidate.splitlines()

        # Further clean-up: explicitly exclude lines containing patent codes/classifications
        abstract_lines = [
            line.strip() for line in abstract_lines
            if line.strip() and not re.match(r"^\(?\s*\d+\s*\)?|[A-Z]+\d+[A-Z]?\s*\d+/\d+", line.strip())
        ]
        abstract = " ".join(abstract_lines).replace('\n', ' ').strip()

    # abstract = ""
    # abstract_match = re.search(
    #     r"abstract\s*[:\-\s]*\n?(.*?)(?:\n\s*(field of invention|technical field|background|summary|claims|description|brief description of drawings)|\n\d+\s*\.)",
    #     full_text, re.IGNORECASE | re.DOTALL
    # )

    # if abstract_match:
    #     abstract_candidate = abstract_match.group(1).strip()
    #     abstract_lines = abstract_candidate.splitlines()

    #     # Further clean-up: exclude lines with classifications or codes
    #     abstract_lines = [
    #         line for line in abstract_lines
    #         if not re.match(r"^\(?\s*\d+\s*\)?|[A-Z]+\d+[A-Z]?\s*\d+/\d+", line.strip())
    #     ]
    #     abstract = " ".join(abstract_lines).replace('\n', ' ').strip()

    #     # Ensure abstract is at least a minimum length, otherwise fallback to OCR
    #     if len(abstract) < 50:
    #         print("[INFO] Abstract too short, triggering OCR fallback...")
    #         ocr_text = extract_text_ocr(file_bytes)
    #         ocr_abstract_match = re.search(
    #             r"abstract\s*[:\-\s]*\n?(.*?)(?:\n\s*(field of invention|technical field|background|summary|claims|description|brief description of drawings)|\n\d+\s*\.)",
    #             ocr_text, re.IGNORECASE | re.DOTALL
    #         )
    #         if ocr_abstract_match:
    #             abstract = ocr_abstract_match.group(1).replace('\n', ' ').strip()


    # --- Claims ---
    claim_lines = []
    found_claim_start = False
    for line in lines:
        if re.match(r"^(1[\.\)]|claim\s*1)", line.lower()):
            found_claim_start = True
        if found_claim_start:
            if re.match(r"^(description|background|abstract)", line.lower()):
                break
            claim_lines.append(line)

    grouped_claims = []
    current = ""
    for line in claim_lines:
        if re.match(r"^\d+[\.\)]", line.strip()):
            if current:
                grouped_claims.append(current.strip())
            current = line
        else:
            current += " " + line
    if current:
        grouped_claims.append(current.strip())

    return {
        "title": title.strip(),
        "abstract": abstract.strip(),
        "claims": grouped_claims,
        "source": source  # NEW: add this so app.py can show in frontend
    }
