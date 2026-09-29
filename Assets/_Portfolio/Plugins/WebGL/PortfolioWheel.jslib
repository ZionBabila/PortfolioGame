mergeInto(LibraryManager.library, {
  // Mouse-wheel / touchpad zoom for the web build. The Input System doesn't receive wheel events on WebGL,
  // so listen on the page ourselves and hand Unity the accumulated amount once per frame.
  // Returns pixels since the last call, positive = wheel up (zoom in).
  PortfolioWheelConsume: function () {
    if (!window.portfolioWheel) {
      window.portfolioWheel = { sum: 0 };
      var canvas = document.querySelector('#unity-canvas') || document.querySelector('canvas');
      (canvas || window).addEventListener('wheel', function (e) {
        var scale = e.deltaMode === 1 ? 40 : e.deltaMode === 2 ? 800 : 1; // lines / pages -> pixels
        window.portfolioWheel.sum -= e.deltaY * scale;
        e.preventDefault(); // don't scroll or browser-zoom the page (ctrl+wheel = touchpad pinch)
      }, { passive: false, capture: true });
    }
    var sum = window.portfolioWheel.sum;
    window.portfolioWheel.sum = 0;
    return sum;
  }
});
