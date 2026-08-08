mergeInto(LibraryManager.library, {

  SaveGet: function(keyPtr) {
    var key = UTF8ToString(keyPtr);
    var val = localStorage.getItem(key);
    if (val === null) val = '';
    var bufSize = lengthBytesUTF8(val) + 1;
    var buf = _malloc(bufSize);
    stringToUTF8(val, buf, bufSize);
    return buf;
  },

  SaveSet: function(keyPtr, valuePtr) {
    localStorage.setItem(UTF8ToString(keyPtr), UTF8ToString(valuePtr));
  },

  SaveRemove: function(keyPtr) {
    localStorage.removeItem(UTF8ToString(keyPtr));
  }

});
