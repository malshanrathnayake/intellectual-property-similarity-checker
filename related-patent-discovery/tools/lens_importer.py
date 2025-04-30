import requests
import json

def fetch_lens_patents(num=100):
    headers = {"Content-Type": "application/json"}
    query = {
        "query": "*",
        "size": num,
        "include": ["lens_id", "title", "abstract"],
        "sort": [{"field": "date_published", "order": "desc"}]
    }

    response = requests.post("https://api.lens.org/patent/search", headers=headers, json=query)
    data = response.json()
    patents = []

    for i, item in enumerate(data.get("data", []), start=1):
        title = item.get("title", {}).get("text", "")
        abstract = item.get("abstract", {}).get("text", "")
        if title and abstract:
            patents.append({
                "id": len(patents) + 1,
                "title": title,
                "abstract": abstract,
                "claims": []
            })

    return patents

if __name__ == "__main__":
    patents = fetch_lens_patents()
    if not patents:
        print("⚠️ No patents fetched! Will not overwrite patents.json.")
    else:
        with open("patents.json", "w", encoding="utf-8") as f:
            json.dump(patents, f, indent=2, ensure_ascii=False)
        print(f"[✓] Saved {len(patents)} patents to patents.json")

