const defaultPosition = {
    longitude: 172.639847,
    latitude: -43.525650,
    zoom: 9
}

const baseUrl = "https://tdp-gis-app-xtfpi.ondigitalocean.app/api"
const searchInstanceUrl = `${baseUrl}/GisQuery/instances`
const searchGeoTypeUrl = `${baseUrl}/GisQuery/querybytext`

const mapboxAccessToken = import.meta.env.VITE_MAPBOX_ACCESS_TOKEN || ""

export { defaultPosition, mapboxAccessToken, searchInstanceUrl, searchGeoTypeUrl }