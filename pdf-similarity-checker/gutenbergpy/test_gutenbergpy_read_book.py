import os
from gutenbergpy.caches.sqlitecache import SQLiteCache
from gutenbergpy.textget import get_text_by_id

# Output path
OUTPUT_FILE = "pride_and_prejudice.txt"

# Check if cache is accessible
cache = SQLiteCache()
print("Cache exists:", cache)

# Load and write text
try:
    text = get_text_by_id(1342).decode('utf-8')

    print("\nSuccessfully loaded 'Pride and Prejudice'")
    print(f"Writing to: {OUTPUT_FILE} ...")

    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        f.write(text)

    print("Text successfully saved!")

except Exception as e:
    print("Failed to load book:", e)
