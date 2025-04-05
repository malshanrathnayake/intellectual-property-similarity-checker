from gutenbergpy.gutenbergcache import GutenbergCache
from gutenbergpy.gutenbergcachesettings import GutenbergCacheSettings
from gutenbergpy.parse.rdfparser import RdfParser
from gutenbergpy.caches.sqlitecache import SQLiteCache
import os

#Override defaults to skip download/unpack
print("Rebuilding SQLite cache using existing RDF files...")

#Ensure test file is ignored (optional but safe)
test_file = os.path.join('cache', 'epub', 'test', 'pgtest.rdf')
if os.path.exists(test_file):
    print("Skipping problematic test RDF file...")
    os.remove(test_file)

#Parse RDFs
parser = RdfParser()
result = parser.do()

#Build SQLite cache
cache = SQLiteCache()
cache.create_cache(result)

print("SQLite metadata cache created successfully!")
print(f"Location: {GutenbergCacheSettings.CACHE_FILENAME}")
