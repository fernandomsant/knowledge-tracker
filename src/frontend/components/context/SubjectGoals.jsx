import { useState } from 'react';
import { CalendarDays, Plus, Target, Trash2 } from '../../icons';

const clamp = value => Math.max(0, Math.min(100, value));
const dateFormatter = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric' });

function GoalProgress({ goal }) {
  if (goal.kind === 1) {
    const percent = clamp((goal.currentValue / goal.targetValue) * 100);
    return <><div className="goal-meta"><span>{goal.metricDefinition.name}</span><strong>{goal.currentValue} / {goal.targetValue}</strong></div><div className="goal-progress"><i style={{ width: `${percent}%` }}/></div><small>{Math.round(percent)}% complete</small></>;
  }
  const target = new Date(`${goal.targetDate}T00:00:00`);
  const start = new Date(goal.createdAtUtc);
  const total = Math.max(1, target - start);
  const elapsed = clamp(((Date.now() - start) / total) * 100);
  const remainingDays = Math.ceil((target - Date.now()) / 86_400_000);
  return <><div className="goal-meta"><span>Due {dateFormatter.format(target)}</span><strong>{remainingDays >= 0 ? `${remainingDays} days left` : `${Math.abs(remainingDays)} days overdue`}</strong></div><div className={`goal-progress ${remainingDays < 0 ? 'overdue' : ''}`}><i style={{ width: `${elapsed}%` }}/></div><small>{remainingDays >= 0 ? 'Time until target date' : 'Target date has passed'}</small></>;
}

export function SubjectGoals({ goals, metricDefinitions, onCreate, onRemove }) {
  const [open, setOpen] = useState(false);
  const [kind, setKind] = useState(1);
  const [title, setTitle] = useState('');
  const [metricDefinitionId, setMetricDefinitionId] = useState('');
  const [targetValue, setTargetValue] = useState('');
  const [targetDate, setTargetDate] = useState('');
  const save = async event => {
    event.preventDefault();
    const goal = kind === 1 ? { title, kind, metricDefinitionId: metricDefinitionId || null, targetValue: Number(targetValue), targetDate: null } : { title, kind, metricDefinitionId: null, targetValue: null, targetDate: targetDate || null };
    if (!title.trim() || (kind === 1 && (!metricDefinitionId || !Number.isFinite(goal.targetValue) || goal.targetValue <= 0)) || (kind === 2 && !targetDate)) return;
    if (await onCreate({ ...goal, title: title.trim() })) { setOpen(false); setTitle(''); setMetricDefinitionId(''); setTargetValue(''); setTargetDate(''); }
  };
  return <section className="subject-goals"><div className="subject-goals-head"><div><span>GOALS</span><small>Track progress for this subject.</small></div><button type="button" className="text-button" onClick={() => setOpen(value => !value)}><Plus size={15}/> Add goal</button></div>
    {open ? <form className="goal-composer" onSubmit={save}><label>Goal title<input value={title} onChange={event => setTitle(event.target.value)} placeholder="What are you aiming for?" maxLength="256"/></label><fieldset><legend>Goal type</legend><label><input type="radio" checked={kind === 1} onChange={() => setKind(1)}/> Metric target</label><label><input type="radio" checked={kind === 2} onChange={() => setKind(2)}/> Target date</label></fieldset>{kind === 1 ? <div className="goal-fields"><select value={metricDefinitionId} onChange={event => setMetricDefinitionId(event.target.value)}><option value="">Choose a metric</option>{metricDefinitions.map(definition => <option key={definition.id} value={definition.id}>{definition.name}</option>)}</select><input type="number" min="0.01" step="0.01" value={targetValue} onChange={event => setTargetValue(event.target.value)} placeholder="Target value"/></div> : <label>Target date<input type="date" value={targetDate} onChange={event => setTargetDate(event.target.value)}/></label>}<div><button type="button" className="ghost-button" onClick={() => setOpen(false)}>Cancel</button><button className="primary-button">Save goal</button></div></form> : null}
    <div className="goal-list">{goals.map(goal => <article className="goal-card" key={goal.id}><div className="goal-title"><span>{goal.kind === 1 ? <Target size={15}/> : <CalendarDays size={15}/>}</span><strong>{goal.title}</strong><button type="button" aria-label={`Remove ${goal.title}`} onClick={() => onRemove(goal.id)}><Trash2 size={14}/></button></div><GoalProgress goal={goal}/></article>)}{goals.length === 0 ? <p className="goal-empty">Set a metric or date goal to see your progress here.</p> : null}</div>
  </section>;
}
