import sqlite3

conn = sqlite3.connect('gutenbergindex.db')
cursor = conn.cursor()

# Show tables
cursor.execute("SELECT name FROM sqlite_master WHERE type='table'")
print("Tables:", cursor.fetchall())

# Sample data from books
cursor.execute("SELECT * FROM books LIMIT 5")
for row in cursor.fetchall():
    print(row)

conn.close()
