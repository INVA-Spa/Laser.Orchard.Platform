function InitializeGoogleMaps() {
    $("[data-map-provider='google']").each(function () {
        var mapId = $(this).attr("data-map-id");
        if (mapId) {
            var functionName = "initialize" + mapId;

            $(document).ready(function () {
                window[functionName]();
            });
        }
    });
}