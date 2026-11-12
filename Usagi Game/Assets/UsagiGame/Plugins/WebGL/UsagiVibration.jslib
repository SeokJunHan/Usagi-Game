mergeInto(LibraryManager.library, {
  UsagiVibrate: function (milliseconds) {
    if (navigator.vibrate) navigator.vibrate(milliseconds);
  }
});
