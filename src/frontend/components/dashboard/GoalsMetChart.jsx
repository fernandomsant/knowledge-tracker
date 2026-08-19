const MAX_DATE_TICKS = 7;
const shortDateFormatter = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', timeZone: 'UTC' });
const datedYearFormatter = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: '2-digit', timeZone: 'UTC' });

const pointX = (index, count, width) => count === 1 ? width / 2 : index / (count - 1) * width;
const formatTickDate = (value, includeYear) => (includeYear ? datedYearFormatter : shortDateFormatter).format(new Date(`${value}T00:00:00Z`));

function buildDateTicks(series, width) {
  const tickCount = Math.min(MAX_DATE_TICKS, series.length);
  if (tickCount === 1) return [{ ...series[0], x: width / 2 }];

  return Array.from({ length: tickCount }, (_, tickIndex) => {
    const seriesIndex = Math.round(tickIndex / (tickCount - 1) * (series.length - 1));
    return { ...series[seriesIndex], x: pointX(seriesIndex, series.length, width) };
  });
}

export function GoalsMetChart({ series }) {
  const width = 720;
  const height = 190;
  const max = Math.max(1, ...series.map(item => item.expected));
  const points = series.map((item, index) => `${pointX(index, series.length, width)},${height - item.met / max * (height - 24)}`).join(' ');

  if (!series.length || !series.some(item => item.expected)) return <div className="behavior-empty"><span>No goal occurrences in this range.</span></div>;

  const dateTicks = buildDateTicks(series, width);
  const includeYear = series[0].date.slice(0, 4) !== series.at(-1).date.slice(0, 4) || series.length > 120;

  return (
    <div className="goals-met-chart" role="img" aria-label="Goals met over time, grouped by occurrence start date">
      <svg viewBox={`0 0 ${width} ${height}`} preserveAspectRatio="none">
        {dateTicks.map(tick => <line key={tick.date} x1={tick.x} y1="0" x2={tick.x} y2={height - 1} stroke="currentColor" strokeDasharray="3 5" opacity=".12"/>)}
        <line x1="0" y1={height - 1} x2={width} y2={height - 1} stroke="currentColor" opacity=".2"/>
        <polyline points={points} fill="none" stroke="currentColor" strokeWidth="3" strokeLinejoin="round" strokeLinecap="round"/>
        {series.map((item, index) => item.met ? <circle key={item.date} cx={pointX(index, series.length, width)} cy={height - item.met / max * (height - 24)} r="4"/> : null)}
      </svg>
      <div className="goals-met-chart-labels" style={{ gridTemplateColumns: `repeat(${dateTicks.length}, minmax(0, 1fr))` }}>
        {dateTicks.map(tick => <span key={tick.date}>{formatTickDate(tick.date, includeYear)}</span>)}
      </div>
      <p>Completed occurrences / expected occurrences, grouped by occurrence start date.</p>
    </div>
  );
}
