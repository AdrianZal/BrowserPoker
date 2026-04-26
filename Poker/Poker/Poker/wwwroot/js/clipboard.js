window.copyToClipboard = (text) => {
    navigator.clipboard.writeText(text).then(() => {
        console.log("Code copied to clipboard");
    }).catch(err => {
        console.error("Could not copy text: ", err);
    });
};