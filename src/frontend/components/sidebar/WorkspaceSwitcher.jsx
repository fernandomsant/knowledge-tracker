import { memo } from 'react';
import { ChevronDown, Plus } from '../../icons';

const MAX_WORKSPACES = 5;

export const WorkspaceSwitcher = memo(function WorkspaceSwitcher({
  user,
  workspaces,
  activeWorkspaceId,
  onChange,
  onCreate,
}) {
  const canCreateWorkspace = workspaces.length < MAX_WORKSPACES;

  return (
    <div className="workspace-switcher">
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
      <button
        type="button"
        className="workspace-create-button"
        onClick={onCreate}
        disabled={!canCreateWorkspace}
        title={canCreateWorkspace ? 'Create workspace' : 'You have reached the five-workspace limit'}
      >
        <Plus size={14} aria-hidden="true" />
        <span>{canCreateWorkspace ? 'New workspace' : 'Workspace limit reached'}</span>
        <small>{workspaces.length}/{MAX_WORKSPACES}</small>
      </button>
    </div>
  );
});
