import requests, json

headers = {"Content-Type": "application/json"}
query = {
    "query": "*",
    "size": 10,
    "include": ["lens_id", "title", "abstract"],
    "sort": [{"field": "date_published", "sort": "desc"}]
}

res = requests.post("https://api.lens.org/scholarly/search", headers=headers, json=query)
print(res.status_code)
print(res.json())
