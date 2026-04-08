# Test static files are served
GET http://localhost:3000/static/css/site.css
HTTP 200

GET http://localhost:3000/static/js/formatDates.js
HTTP 200
