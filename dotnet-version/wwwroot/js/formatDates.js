const options = {
    dateStyle: "medium",
    timeStyle: "medium",
};

function reformatElement(e) {
    const src = e.attributes["data-datetime"].value;
    const dt = new Date(Date.parse(src));
    e.innerText = dt.toLocaleString(navigator.language, options);
}

const dtElements = document.querySelectorAll("[data-datetime]");
dtElements.forEach(reformatElement);
