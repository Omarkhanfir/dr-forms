mergeInto(LibraryManager.library, {
  PrototypeDiagnostics_Report: function (json) {
    if (window.DrFormsDiagnostics && window.DrFormsDiagnostics.receiveUnity)
      window.DrFormsDiagnostics.receiveUnity(JSON.parse(UTF8ToString(json)));
  }
});
