let map;
let service;
let infowindow;

console.log("map.js loaded");

document.addEventListener("DOMContentLoaded", () => {

    const btn = document.getElementById("find-gas-button");

    if (!btn) {
        console.log("button not found");
        return;
    }

    btn.addEventListener("click", () => {

        console.log("clicked");

        if (!navigator.geolocation) {
            alert("Geolocation is not supported");
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (position) => {

                const lat = position.coords.latitude;
                const lng = position.coords.longitude;

                initMap(lat, lng);

            },
            (error) => {
                console.log(error);
                alert("位置情報取得失敗: " + error.message);
            }
        );
    });
});


function initMap(lat, lng) {

    const location = { lat, lng };

    map = new google.maps.Map(document.getElementById("map"), {
        center: location,
        zoom: 15
    });

    // 現在地マーカー
    new google.maps.Marker({
        position: location,
        map: map,
        title: "現在地"
    });

    infowindow = new google.maps.InfoWindow();

    service = new google.maps.places.PlacesService(map);

    const request = {
        location: location,
        radius: 1500,
        type: "gas_station"
    };

    service.nearbySearch(request, (results, status) => {

        if (status === google.maps.places.PlacesServiceStatus.OK) {

            results.forEach(createMarker);

        } else {
            console.log("Places error:", status);
            alert("ガソリンスタンド取得失敗: " + status);
        }
    });
}

function createMarker(place) {

    if (!place.geometry || !place.geometry.location) return;

    const marker = new google.maps.Marker({
        map: map,
        position: place.geometry.location,
        title: place.name
    });

    marker.addListener("click", () => {

        infowindow.setContent(place.name);
        infowindow.open(map, marker);

    });
}