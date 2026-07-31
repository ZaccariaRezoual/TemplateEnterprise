/**
 * Locale-aware formatting helpers.
 *
 * All of them take an explicit locale with a documented default: relying on
 * the ambient system locale makes output non-deterministic across machines and
 * breaks tests and server-side rendering. Formatter instances are cached
 * because constructing `Intl` objects is comparatively expensive.
 */

const numberFormatters = new Map<string, Intl.NumberFormat>();
const dateFormatters = new Map<string, Intl.DateTimeFormat>();

/** Locale used when a caller does not specify one. */
export const DEFAULT_LOCALE = "en-US";

/**
 * Formats a number.
 *
 * @param value Number to format.
 * @param options.locale BCP 47 locale tag (defaults to {@link DEFAULT_LOCALE}).
 * @param options.maximumFractionDigits Maximum decimals to display.
 * @returns The formatted number.
 */
export function formatNumber(
  value: number,
  options: { locale?: string; maximumFractionDigits?: number } = {},
): string {
  const { locale = DEFAULT_LOCALE, maximumFractionDigits } = options;
  const key = `${locale}:${maximumFractionDigits ?? "auto"}`;

  let formatter = numberFormatters.get(key);
  if (formatter === undefined) {
    formatter = new Intl.NumberFormat(
      locale,
      maximumFractionDigits === undefined ? {} : { maximumFractionDigits },
    );
    numberFormatters.set(key, formatter);
  }

  return formatter.format(value);
}

/**
 * Formats an amount of money.
 *
 * @param value Amount in major units (e.g. euros, not cents).
 * @param currency ISO 4217 code, such as "EUR".
 * @param locale BCP 47 locale tag (defaults to {@link DEFAULT_LOCALE}).
 * @returns The formatted amount including the currency symbol.
 */
export function formatCurrency(value: number, currency: string, locale = DEFAULT_LOCALE): string {
  const key = `${locale}:currency:${currency}`;

  let formatter = numberFormatters.get(key);
  if (formatter === undefined) {
    formatter = new Intl.NumberFormat(locale, { style: "currency", currency });
    numberFormatters.set(key, formatter);
  }

  return formatter.format(value);
}

/**
 * Formats a date or an ISO 8601 string.
 *
 * @param value Date instance or ISO string (the API always sends ISO UTC).
 * @param options.locale BCP 47 locale tag (defaults to {@link DEFAULT_LOCALE}).
 * @param options.dateStyle Intl date style.
 * @param options.timeStyle Intl time style; omit for date-only output.
 * @returns The formatted date, or an empty string when the value is not a valid date.
 */
export function formatDate(
  value: Date | string,
  options: {
    locale?: string;
    dateStyle?: Intl.DateTimeFormatOptions["dateStyle"];
    timeStyle?: Intl.DateTimeFormatOptions["timeStyle"];
  } = {},
): string {
  const { locale = DEFAULT_LOCALE, dateStyle = "medium", timeStyle } = options;
  const date = value instanceof Date ? value : new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "";
  }

  const key = `${locale}:${dateStyle}:${timeStyle ?? "none"}`;
  let formatter = dateFormatters.get(key);
  if (formatter === undefined) {
    formatter = new Intl.DateTimeFormat(locale, {
      dateStyle,
      ...(timeStyle === undefined ? {} : { timeStyle }),
    });
    dateFormatters.set(key, formatter);
  }

  return formatter.format(date);
}
