mergeInto(LibraryManager.library, {

  Share_Try: function(titlePtr, textPtr, urlPtr) {
    var title = UTF8ToString(titlePtr);
    var text  = UTF8ToString(textPtr);
    var url   = UTF8ToString(urlPtr);
    var full  = text + '\n' + url;

    if (navigator.share) {
      navigator.share({ title: title, text: text, url: url }).catch(function(e) {
        if (e && e.name !== 'AbortError') {
          if (navigator.clipboard) {
            navigator.clipboard.writeText(full).catch(function() { window.prompt('링크 복사:', url); });
          } else {
            window.prompt('링크 복사:', url);
          }
        }
      });
    } else if (navigator.clipboard) {
      navigator.clipboard.writeText(full).then(function() {
        alert('결과가 클립보드에 복사되었습니다!');
      }).catch(function() {
        window.prompt('링크 복사:', url);
      });
    } else {
      window.prompt('링크 복사:', url);
    }
  }

});
