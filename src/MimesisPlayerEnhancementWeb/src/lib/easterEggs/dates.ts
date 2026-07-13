export function isCalendarDay(date: Date, month: number, day: number): boolean {
  return date.getMonth() + 1 === month && date.getDate() === day;
}

export function isAnyCalendarDay(date: Date, days: Array<[month: number, day: number]>): boolean {
  return days.some(([month, day]) => isCalendarDay(date, month, day));
}

export function isInRange(
  date: Date,
  startMonth: number,
  startDay: number,
  endMonth: number,
  endDay: number,
): boolean {
  const year = date.getFullYear();
  const current = new Date(year, date.getMonth(), date.getDate()).getTime();
  const start = new Date(year, startMonth - 1, startDay).getTime();
  const end = new Date(year, endMonth - 1, endDay).getTime();
  return current >= start && current <= end;
}

export function isDayOfYear(date: Date, dayOfYear: number): boolean {
  const start = new Date(date.getFullYear(), 0, 0);
  const diff = date.getTime() - start.getTime();
  const oneDay = 1000 * 60 * 60 * 24;
  return Math.floor(diff / oneDay) === dayOfYear;
}
