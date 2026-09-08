import { memo } from 'react';
import { ChevronDown } from '../../icons';

export const WorkspaceSwitcher = memo(function WorkspaceSwitcher({
  user,
  workspaces,
  activeWorkspaceId,
  onChange,
}) {
  return (
    <label className="workspace-switcher">
      <span className="avatar">{user.login.slice(0, 2).toUpperCase()}</span>
      <span className="workspace-switcher-copy">
        <strong>{user.login}</strong>
        <select
          aria-label="Switch workspace"
          value={activeWorkspaceId ?? ''}
          onChange={event => onChange(event.target.value)}
          disabled={!workspaces.length}
        >
          {workspaces.length
            ? workspaces.map(workspace => <option key={workspace.id} value={workspace.id}>{workspace.name}</option>)
            : <option value="">No workspaces</option>}
        </select>
      </span>
      <ChevronDown size={16} aria-hidden="true" />
    </label>
  );
});
