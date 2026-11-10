mergeInto(LibraryManager.library, {
  // navigator.vibrate works on Android browsers; silently ignored elsewhere (e.g. iOS Safari).
  Haptic_Vibrate: function (ms) {
    try { if (navigator.vibrate) navigator.vibrate(ms); } catch (e) {}
  }
});
