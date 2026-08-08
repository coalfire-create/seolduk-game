mergeInto(LibraryManager.library, {

  GA4_Event: function(namePtr, paramsJsonPtr) {
    if (typeof gtag === 'undefined') return;
    var name = UTF8ToString(namePtr);
    var params = {};
    try { params = JSON.parse(UTF8ToString(paramsJsonPtr)); } catch(e) {}
    gtag('event', name, params);
  }

});
