# Test summary page for lobste.rs
GET http://localhost:3000/summary/lobste.rs
HTTP 200
[Asserts]
body contains "lobste.rs"
body contains "Go To Date"

# Test summary page for HN
GET http://localhost:3000/summary/news.ycombinator.com
HTTP 200
[Asserts]
body contains "news.ycombinator.com"

# Test summary with date parameter
GET http://localhost:3000/summary/lobste.rs?date=2024-01-15
HTTP 200
[Asserts]
body contains "2024-01-15"
body contains "Go to latest"

# Test 404 for unknown site
GET http://localhost:3000/summary/nonexistent
HTTP 404
