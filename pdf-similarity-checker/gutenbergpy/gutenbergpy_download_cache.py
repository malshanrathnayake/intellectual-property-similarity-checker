from gutenbergpy.gutenbergcache import GutenbergCache

# This will download the catalog and build the cache
print("Downloading and building the metadata cache (may take a few minutes)...")
GutenbergCache.create()
print("Done!")
