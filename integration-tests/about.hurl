# Test about page
GET http://localhost:3000/about
HTTP 200
[Asserts]
body contains "Slowcial Sharing"
body contains "RSS feeds"
body contains "Last Update"
body contains "Next Update"
