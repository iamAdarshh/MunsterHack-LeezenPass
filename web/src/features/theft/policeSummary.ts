import i18n from '../../i18n'
import type { Bike, Theft } from '../../api/types'

export const internetwacheUrl = 'https://polizei.nrw/internetwache'

/**
 * Text to paste into the Polizei NRW Internetwache form. Always German (it goes to the German police),
 * whatever language the UI is in.
 */
export function policeSummary(bike: Bike, theft: Theft, origin: string): string {
  const t = i18n.getFixedT('de')
  const stolenAt = new Date(theft.stolenAt).toLocaleString('de-DE', { dateStyle: 'medium', timeStyle: 'short' })
  const colors = [bike.colorPrimary, bike.colorSecondary].filter((c) => c !== null).map((c) => t(`bikeColor.${c}`))
  const lines = [
    'Fahrraddiebstahl – Angaben für die Anzeige',
    '',
    `Tatzeit (ungefähr): ${stolenAt} Uhr`,
    `Tatort: ${theft.locationNote ? `${theft.locationNote}, ` : ''}Münster-${t(`district.${theft.district}`)}`,
    `Koordinaten: ${theft.latitude.toFixed(5)}, ${theft.longitude.toFixed(5)}`,
    `Schloss: ${t(`lockType.${theft.lockType}`)}`,
    '',
    `Fahrrad: ${[t(`bikeType.${bike.type}`), bike.brand, bike.model].filter(Boolean).join(', ')}`,
    `Rahmennummer: ${bike.frameNumber}`,
    `Farbe: ${colors.join(' / ') || '–'}`,
    `Merkmale: ${bike.features.map((f) => t(`bikeFeature.${f}`)).join(', ') || '–'}`,
    `E-Bike: ${bike.isEbike ? `ja${bike.batterySerial ? `, Akku-Nr. ${bike.batterySerial}` : ''}` : 'nein'}`,
    bike.purchaseDate ? `Kaufdatum: ${new Date(bike.purchaseDate).toLocaleDateString('de-DE')}` : null,
    bike.hasFeinCode ? 'FEIN-Code: vorhanden (bitte ergänzen)' : null,
    '',
    `Fahrradpass (LeezenPass): ${origin}/b/${bike.publicToken}`,
  ]
  return lines.filter((line) => line !== null).join('\n')
}
