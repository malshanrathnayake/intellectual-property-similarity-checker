import fitz  # PyMuPDF
import pytesseract
import os
from pdf2image import convert_from_bytes
import re

# Set Tesseract path
if os.name == "nt":  # Windows
    pytesseract.pytesseract.tesseract_cmd = r"C:\Program Files\Tesseract-OCR\tesseract.exe"
else:  # Linux (Azure)
    pytesseract.pytesseract.tesseract_cmd = "/usr/bin/tesseract"

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
# Extract text using OCR (IMPROVED)
# ---------------------------------------------------------
def extract_text_ocr(file_bytes: bytes, max_pages=5) -> str:
    """OCR with better error handling and validation"""
    try:
        print("[OCR] Converting PDF to images...")
        images = convert_from_bytes(file_bytes, dpi=300, first_page=1, last_page=max_pages)
        
        if not images:
            print("[OCR] No images converted from PDF")
            return ""
        
        ocr_result = []
        for i, img in enumerate(images):
            print(f"[OCR] Processing page {i+1}/{len(images)}...")
            
            # Try multiple OCR configs for better results
            configs = [
                '--psm 6',  # Uniform block of text
                '--psm 3',  # Fully automatic page segmentation
                '--psm 4',  # Single column of text
            ]
            
            best_text = ""
            for config in configs:
                try:
                    text = pytesseract.image_to_string(img, config=config)
                    if len(text.strip()) > len(best_text.strip()):
                        best_text = text
                except Exception as e:
                    print(f"[OCR] Config {config} failed: {e}")
                    continue
            
            if len(best_text.strip()) > 50:  # Lowered threshold
                ocr_result.append(best_text.strip())
                print(f"[OCR] Page {i+1}: Extracted {len(best_text)} chars")
            else:
                print(f"[OCR] Page {i+1}: Insufficient text ({len(best_text)} chars)")
        
        final_text = "\n\n".join(ocr_result)
        print(f"[OCR] Total extracted: {len(final_text)} characters")
        return final_text
    
    except Exception as e:
        print(f"[OCR] Complete failure: {e}")
        return ""

# ---------------------------------------------------------
# Clean text
# ---------------------------------------------------------
def clean_text(text: str) -> str:
    """Remove patent metadata noise"""
    lines = []
    for line in text.splitlines():
        line = line.strip()
        if not line:
            continue
        
        # Skip headers/footers
        if re.match(r'^(?:'
                   r'Page\s+\d+|'
                   r'\d+\s+of\s+\d+|'
                   r'US\s*\d{7,}|'
                   r'WO\s*\d{4}/\d+|'
                   r'Patent\s*No\.|'
                   r'\(\d+\)\s*(?:Patent|Date|Field|Int\.Cl\.|U\.S\.Cl\.)|'
                   r'[A-Z]\d+[A-Z]\s+\d+/\d+|'
                   r'References\s+Cited|'
                   r'Prior\s+Publication|'
                   r'Foreign\s+Application'
                   r')', line, re.I):
            continue
        
        # Skip pure numbers/dates
        if re.match(r'^[\d\s\(\)\.\-/]+$', line):
            continue
        
        lines.append(line)
    return "\n".join(lines)

# ---------------------------------------------------------
# Extract Title (IMPROVED)
# ---------------------------------------------------------
def extract_title(text: str) -> str:
    """Extract patent title - handles multi-line titles"""
    
    # Strategy 1: (54) marker with potential multi-line title
    match = re.search(r'\(\s*54\s*\)\s*([A-Z][^\(]+?)(?=\(\d+\)|\n\s*\n)', text, re.I | re.DOTALL)
    if match:
        title = match.group(1).strip()
        title = re.sub(r'\s+', ' ', title)
        title = re.split(r'\s+\(\d+\)', title)[0].strip()
        if 10 < len(title) < 300:
            print(f"[Title] Found via (54): {title[:50]}...")
            return title
    
    # Strategy 2: Look for all-caps title (may span multiple lines)
    lines = text.split('\n')[:50]
    title_lines = []
    in_title = False
    
    for line in lines:
        line = line.strip()
        
        if not in_title and line.isupper() and len(line) > 10:
            if not re.match(r'^(ABSTRACT|BACKGROUND|FIELD|SUMMARY|CLAIMS|DESCRIPTION)', line):
                in_title = True
                title_lines.append(line)
        elif in_title:
            if line.isupper() or (line and line[0].isupper()):
                if re.match(r'^(ABSTRACT|BACKGROUND|FIELD|SUMMARY|CLAIMS|DESCRIPTION)', line):
                    break
                title_lines.append(line)
            else:
                break
        
        if in_title and len(' '.join(title_lines)) > 300:
            break
    
    if title_lines:
        title = ' '.join(title_lines)
        title = re.sub(r'\s+', ' ', title)
        if 10 < len(title) < 300:
            print(f"[Title] Found via all-caps: {title[:50]}...")
            return title
    
    # Strategy 3: First substantial line
    cleaned_lines = clean_text(text).split('\n')
    for line in cleaned_lines[:30]:
        if 10 < len(line) < 300 and line[0].isupper():
            print(f"[Title] Found via first line: {line[:50]}...")
            return line
    
    print("[Title] Using fallback title")
    return "Untitled Patent"

# ---------------------------------------------------------
# Extract Abstract (SIGNIFICANTLY IMPROVED)
# ---------------------------------------------------------
def extract_abstract(text: str) -> str:
    """Extract abstract with multiple strategies"""
    
    print("[Abstract] Starting extraction...")
    
    # Strategy 1: Standard (57) ABSTRACT format
    pattern1 = r'\(\s*57\s*\)\s*ABSTRACT\s*\n+(.*?)(?=\n\s*(?:\(\d+\)|BACKGROUND|FIELD|SUMMARY|CLAIMS|DESCRIPTION|BRIEF|DETAILED|What is claimed))'
    match = re.search(pattern1, text, re.I | re.DOTALL)
    if match:
        abstract = clean_abstract_text(match.group(1).strip())
        if len(abstract) >= 50:
            print(f"[Abstract] Found via (57) pattern: {len(abstract)} chars")
            return abstract
    
    # Strategy 2: ABSTRACT header followed by content
    pattern2 = r'ABSTRACT\s*\n+(.*?)(?=\n\s*(?:BACKGROUND|FIELD|SUMMARY|CLAIMS|DESCRIPTION|BRIEF|DETAILED|\d+\s+Claims?|FIG\.))'
    match = re.search(pattern2, text, re.I | re.DOTALL)
    if match:
        abstract = clean_abstract_text(match.group(1).strip())
        if len(abstract) >= 50:
            print(f"[Abstract] Found via ABSTRACT header: {len(abstract)} chars")
            return abstract
    
    # Strategy 3: More lenient - grab content after ABSTRACT keyword
    abstract_start = re.search(r'\bABSTRACT\b', text, re.I)
    if abstract_start:
        text_from_abstract = text[abstract_start.end():]
        
        # Take next 500-3000 chars and clean
        candidate = text_from_abstract[:3000]
        
        # Stop at section headers
        for header in ['BACKGROUND', 'FIELD', 'SUMMARY', 'CLAIMS', 'DESCRIPTION', 'BRIEF', 'DETAILED']:
            header_match = re.search(rf'\b{header}\b', candidate, re.I)
            if header_match:
                candidate = candidate[:header_match.start()]
                break
        
        abstract = clean_abstract_text(candidate.strip())
        if len(abstract) >= 50:
            print(f"[Abstract] Found via lenient pattern: {len(abstract)} chars")
            return abstract
    
    # Strategy 4: Look between (57) and next section
    pattern4 = r'\(\s*57\s*\)(.*?)(?=\(\d+\)|BACKGROUND|FIELD|SUMMARY|CLAIMS)'
    match = re.search(pattern4, text, re.I | re.DOTALL)
    if match:
        abstract = clean_abstract_text(match.group(1).strip())
        if len(abstract) >= 50:
            print(f"[Abstract] Found via (57) to section: {len(abstract)} chars")
            return abstract
    
    # Strategy 5: Find paragraphs after first page that look like abstracts
    # (typically 100-2000 chars, complete sentences)
    paragraphs = re.split(r'\n\s*\n', text[:5000])
    for para in paragraphs:
        para_clean = clean_abstract_text(para)
        # Check if it looks like an abstract
        if (100 <= len(para_clean) <= 2000 and 
            para_clean.count('.') >= 2 and  # Has multiple sentences
            not re.match(r'^(?:BACKGROUND|FIELD|SUMMARY|CLAIMS)', para_clean, re.I)):
            print(f"[Abstract] Found via paragraph analysis: {len(para_clean)} chars")
            return para_clean
    
    print("[Abstract] No valid abstract found")
    return ""

def clean_abstract_text(abstract: str) -> str:
    """Clean up extracted abstract text"""
    
    # Remove "ABSTRACT" keyword if present at start
    abstract = re.sub(r'^ABSTRACT[:\-\s]*', '', abstract, flags=re.I)
    
    # Remove (57) and similar markers
    abstract = re.sub(r'^\(\s*\d+\s*\)\s*', '', abstract)
    
    # Fix line breaks within sentences
    abstract = re.sub(r'(?<=[a-z,])\s*\n\s*(?=[a-z])', ' ', abstract)
    
    # Normalize whitespace
    abstract = re.sub(r'\s+', ' ', abstract)
    
    # Remove common metadata patterns
    abstract = re.sub(r'\(\s*\d+\s*\)', '', abstract)
    abstract = re.sub(r'[A-Z]\d+[A-Z]?\s+\d+/\d+', '', abstract)
    abstract = re.sub(r'\d{4}\.\d{2}', '', abstract)
    
    # Remove very long uppercase sequences (likely OCR errors)
    abstract = re.sub(r'\b[A-Z]{15,}\b', '', abstract)
    
    # Remove lines that are all caps (likely headers)
    lines = abstract.split('\n')
    filtered_lines = [line for line in lines if not (line.isupper() and len(line) > 10)]
    abstract = ' '.join(filtered_lines)
    
    # Filter to keep only substantial sentences
    sentences = re.split(r'(?<=[.!?])\s+', abstract)
    cleaned_sentences = [
        s.strip() for s in sentences 
        if len(s.strip()) > 20 and not re.match(r'^[A-Z\d\s/\(\)]+$', s.strip())
    ]
    
    result = ' '.join(cleaned_sentences).strip()
    
    return result

# ---------------------------------------------------------
# Extract Claims (IMPROVED)
# ---------------------------------------------------------
def extract_claims(text: str) -> list:
    """Extract patent claims with better pattern matching"""
    
    # Find claims section
    claims_match = re.search(
        r'(?:^|\n)\s*(?:What is claimed|CLAIMS?|We claim)[:\-\s]*\n+(.*?)(?=\n\s*(?:\*\s*\*\s*\*|ABSTRACT|DESCRIPTION|DRAWINGS|$))',
        text, re.I | re.DOTALL | re.M
    )
    
    if not claims_match:
        print("[Claims] No claims section found")
        return []
    
    claims_text = claims_match.group(1)
    
    # Find numbered claims
    claim_pattern = r'(?:^|\n)\s*(\d+)\s*[\.\)]'
    matches = list(re.finditer(claim_pattern, claims_text, re.M))
    
    if not matches:
        print("[Claims] No numbered claims found")
        return []
    
    claims = []
    for i, match in enumerate(matches):
        claim_num = match.group(1)
        start = match.end()
        end = matches[i + 1].start() if i + 1 < len(matches) else len(claims_text)
        
        claim_text = claims_text[start:end].strip()
        claim_text = re.sub(r'\s+', ' ', claim_text)
        
        if len(claim_text) > 20:
            claims.append(f"{claim_num}. {claim_text}")
    
    print(f"[Claims] Extracted {len(claims)} claims")
    return claims

# ---------------------------------------------------------
# Main Extraction (SIGNIFICANTLY IMPROVED)
# ---------------------------------------------------------
def extract_patent_sections(file_bytes: bytes) -> dict:
    """Main extraction with robust fallback logic"""
    
    print("="*60)
    print("[Extraction] Starting patent extraction...")
    print("="*60)
    
    # Step 1: Try PyMuPDF first
    print("[Extraction] Attempting PyMuPDF extraction...")
    fitz_text = extract_text_fitz(file_bytes).strip()
    print(f"[Extraction] PyMuPDF extracted: {len(fitz_text)} characters")
    
    # Step 2: Analyze quality of extracted text
    if fitz_text:
        alpha_ratio = len(re.findall(r'[A-Za-z]', fitz_text)) / max(len(fitz_text), 1)
        line_count = fitz_text.count('\n')
        print(f"[Extraction] Text quality - Alpha ratio: {alpha_ratio:.2f}, Lines: {line_count}")
    else:
        alpha_ratio = 0
        line_count = 0
    
    # Determine if OCR is needed
    needs_ocr = (
        len(fitz_text) < 500 or
        line_count < 10 or
        alpha_ratio < 0.3
    )
    
    # Step 3: Initial extraction attempt
    full_text = fitz_text
    source = "PDF"
    
    print("[Extraction] Attempting title extraction...")
    title = extract_title(full_text)
    
    print("[Extraction] Attempting abstract extraction...")
    abstract = extract_abstract(full_text)
    
    print("[Extraction] Attempting claims extraction...")
    claims = extract_claims(full_text)
    
    # Step 4: If abstract is missing or too short, try OCR
    if not abstract or len(abstract) < 50:
        print(f"[Extraction] Abstract insufficient ({len(abstract)} chars), triggering OCR...")
        
        ocr_text = extract_text_ocr(file_bytes)
        
        if len(ocr_text) >= 200:
            print(f"[Extraction] OCR successful: {len(ocr_text)} characters")
            
            # Try extracting abstract from OCR text
            abstract_ocr = extract_abstract(ocr_text)
            
            if len(abstract_ocr) > len(abstract):
                print(f"[Extraction] Using OCR abstract: {len(abstract_ocr)} chars")
                abstract = abstract_ocr
                source = "PDF + OCR"
            
            # Also try to improve title if needed
            if title == "Untitled Patent":
                title_ocr = extract_title(ocr_text)
                if title_ocr != "Untitled Patent":
                    title = title_ocr
            
            # Also try to improve claims if needed
            if not claims:
                claims_ocr = extract_claims(ocr_text)
                if claims_ocr:
                    claims = claims_ocr
        else:
            print(f"[Extraction] OCR produced insufficient text: {len(ocr_text)} chars")
    
    # Step 5: Final validation and results
    print("="*60)
    print("[Extraction] Final Results:")
    print(f"  Title: {title[:50]}{'...' if len(title) > 50 else ''}")
    print(f"  Abstract: {len(abstract)} characters")
    print(f"  Claims: {len(claims)} found")
    print(f"  Source: {source}")
    print("="*60)
    
    result = {
        "title": title.strip(),
        "abstract": abstract.strip(),
        "claims": claims,
        "source": source
    }
    
    # Add warning if abstract is still missing
    if not abstract or len(abstract) < 50:
        result["error"] = f"Abstract extraction failed (only {len(abstract)} chars extracted)"
        print(f"[Extraction] WARNING: {result['error']}")
    
    return result