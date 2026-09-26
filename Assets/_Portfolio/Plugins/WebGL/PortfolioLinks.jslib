mergeInto(LibraryManager.library, {
  // Opens the URL on the next DOM pointerup so it runs inside a user gesture and isn't popup-blocked.
  PortfolioOpenOnPointerUp: function (urlPtr) {
    var url = UTF8ToString(urlPtr);
    var open = function () {
      document.removeEventListener('pointerup', open, true);
      var win = window.open(url, '_blank', 'noopener');
      if (!win) window.location.href = url;
    };
    document.addEventListener('pointerup', open, true);
  }
});
