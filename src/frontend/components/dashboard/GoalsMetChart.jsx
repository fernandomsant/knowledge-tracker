import { dateLabel } from './dashboardData';

export function GoalsMetChart({ series }) {
  const width = 720;
  const height = 190;
  const max = Math.max(1, ...series.map(item => item.expected));
  const points = series.map((item, index) => `${series.length === 1 ? width / 2 : index / (series.length - 1) * width},${height - item.met / max * (height - 24)}`).join(' ');
  if (!series.length || !series.some(item => item.expected)) return <div className="behavior-empty"><span>No goal occurrences in this range.</span></div>;
  return <div className="goals-met-chart" role="img" aria-label="Goals met over time, grouped by occurrence start date"><svg viewBox={`0 0 ${width} ${height}`} preserveAspectRatio="none"><line x1="0" y1={height - 1} x2={width} y2={height - 1} stroke="currentColor" opacity=".2"/><polyline points={points} fill="none" stroke="currentColor" strokeWidth="3" strokeLinejoin="round" strokeLinecap="round"/>{series.map((item, index) => item.met ? <circle key={item.date} cx={series.length === 1 ? width / 2 : index / (series.length - 1) * width} cy={height - item.met / max * (height - 24)} r="4"/> : null)}</svg><div className="goals-met-chart-labels"><span>{dateLabel(series[0].date)}</span><span>{dateLabel(series.at(-1).date)}</span></div><p>Completed occurrences / expected occurrences, grouped by occurrence start date.</p></div>;
}
