window.cookieHelper = {
    getCookie: function () {
        let match = document.cookie.match(new RegExp('(^| )' + 'userName' + '=([^;]+)'));
        if (match) {
            return match[2];
        } else {
            return null;
        }
    },
    setCookie: function (value) {
        // Concatenate the name and value of the cookie
        let cookieString = 'userName' + "=" + value;
        // Append the expiration time to the cookie string
        cookieString += "; expires=Session"; // Cookie expires when browser session ends
        // Set the path where the cookie is valid
        cookieString += "; path=/";
        // SameSite attribute: restricts the cookie to first-party context
        cookieString += "; SameSite=Strict";
        // Set the cookie
        document.cookie = cookieString;
    },
    deleteCookie: function () {
        document.cookie = 'userName' + '=; Max-Age=-99999999;';
    }
};
