window.scrollMessagesToBottom = () => {
    const el = document.getElementById('messages');
    if (el) {
        el.scrollTop = el.scrollHeight;
    }
};
