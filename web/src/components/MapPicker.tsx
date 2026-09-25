import 'leaflet/dist/leaflet.css'
import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { CircleMarker, MapContainer, TileLayer, useMap, useMapEvents } from 'react-leaflet'
import { Spinner } from './States'

export interface LatLng {
  latitude: number
  longitude: number
}

const muenster: [number, number] = [51.9607, 7.6261]

interface Props {
  value: LatLng | null
  onChange: (value: LatLng) => void
}

/**
 * Tap to drop a pin. OSM tiles need network; offline the map stays blank but tapping and
 * "use my location" (GPS) still work.
 */
export function MapPicker({ value, onChange }: Props) {
  const { t } = useTranslation()
  const [locating, setLocating] = useState(false)
  const [geoError, setGeoError] = useState(false)

  const locate = () => {
    if (!('geolocation' in navigator)) {
      setGeoError(true)
      return
    }
    setLocating(true)
    setGeoError(false)
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setLocating(false)
        onChange({ latitude: pos.coords.latitude, longitude: pos.coords.longitude })
      },
      () => {
        setLocating(false)
        setGeoError(true)
      },
      { enableHighAccuracy: true, timeout: 10_000 },
    )
  }

  return (
    <div className="space-y-2">
      <div className="isolate h-64 overflow-hidden rounded-lg border border-slate-300">
        <MapContainer center={value ? [value.latitude, value.longitude] : muenster} zoom={13} className="size-full">
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
          />
          <ClickHandler onChange={onChange} />
          {value && (
            <>
              <CircleMarker
                center={[value.latitude, value.longitude]}
                radius={10}
                pathOptions={{ color: '#b91c1c', fillColor: '#dc2626', fillOpacity: 0.8 }}
              />
              <FollowValue value={value} />
            </>
          )}
        </MapContainer>
      </div>
      <button
        type="button"
        onClick={locate}
        disabled={locating}
        className="inline-flex min-h-11 items-center gap-2 text-sm font-semibold text-brand-700"
      >
        {locating && <Spinner className="size-4" />}
        {t('map.useMyLocation')}
      </button>
      {geoError && <p className="text-sm text-red-700">{t('map.locationFailed')}</p>}
    </div>
  )
}

function ClickHandler({ onChange }: { onChange: (value: LatLng) => void }) {
  useMapEvents({
    click: (e) => onChange({ latitude: e.latlng.lat, longitude: e.latlng.lng }),
  })
  return null
}

/** Pans to the pin when it is set from outside (GPS button). */
function FollowValue({ value }: { value: LatLng }) {
  const map = useMap()
  useEffect(() => {
    if (!map.getBounds().contains([value.latitude, value.longitude])) {
      map.setView([value.latitude, value.longitude], Math.max(map.getZoom(), 15))
    }
  }, [map, value.latitude, value.longitude])
  return null
}
