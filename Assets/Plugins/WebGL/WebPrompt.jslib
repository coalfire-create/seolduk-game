mergeInto(LibraryManager.library, {

  // window.prompt로 네이티브 입력을 받는다 (WebGL에서 한글 IME 문제 없이 닉네임 입력).
  // 사용자가 취소하면 빈 문자열 반환.
  Web_Prompt: function(msgPtr, defPtr) {
    var msg = UTF8ToString(msgPtr);
    var def = UTF8ToString(defPtr);
    var res = window.prompt(msg, def);
    if (res === null) res = '';
    var size = lengthBytesUTF8(res) + 1;
    var buf = _malloc(size);
    stringToUTF8(res, buf, size);
    return buf;
  }

});
