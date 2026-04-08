# Test index page loads and lists sites
GET http://localhost:3000/
HTTP 200
[Asserts]
body contains "Slowcial Sharing"
body contains "lobste.rs"
body contains "news.ycombinator.com"
