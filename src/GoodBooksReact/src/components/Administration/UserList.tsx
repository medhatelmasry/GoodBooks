import * as React from 'react';
import { observer } from 'mobx-react';
import AdministrationStore, { User, Role } from '../Shared/Stores/Administration/AdministrationStore';

const store = new AdministrationStore();

// --- Per-user row with role assignment controls ---
type UserRowState = {
    selectedRoleId: number;
};

type UserRowProps = {
    store: AdministrationStore;
    user: User;
};

class UserRow extends React.Component<UserRowProps, UserRowState> {
    constructor(props: UserRowProps) {
        super(props);
        this.state = { selectedRoleId: 0 };
    }

    handleAssign = () => {
        const { selectedRoleId } = this.state;
        if (selectedRoleId === 0) return;
        const { user, store } = this.props;
        const alreadyHas = user.roles.some(r => r.id === selectedRoleId);
        if (alreadyHas) {
            alert('User already has this role.');
            return;
        }
        store.assignRoleToUser(user.id, selectedRoleId);
    };

    handleRemoveRole = (roleId: number) => {
        const { user, store } = this.props;
        store.removeRoleFromUser(user.id, roleId);
    };

    getAvailableRoles(): Role[] {
        const { user, store } = this.props;
        const assignedIds = new Set(user.roles.map(r => r.id));
        return store.roles.filter(r => !assignedIds.has(r.id));
    }

    render() {
        const { user } = this.props;
        const { selectedRoleId } = this.state;
        const availableRoles = this.getAvailableRoles();

        return (
            <tr>
                <td>{user.firstName} {user.lastName}</td>
                <td>{user.email}</td>
                <td>
                    {user.roles.map(role => (
                        <span key={role.id} className="badge bg-secondary me-1" style={{ fontSize: '0.85em' }}>
                            {role.displayName || role.name}
                            <button
                                type="button"
                                className="btn-close btn-close-white ms-1"
                                style={{ fontSize: '0.6em' }}
                                aria-label="Remove"
                                onClick={() => this.handleRemoveRole(role.id)}
                                title={'Remove ' + (role.displayName || role.name)}
                            />
                        </span>
                    ))}
                </td>
                <td>
                    <div className="d-flex gap-2">
                        <select
                            className="form-select form-select-sm"
                            value={selectedRoleId}
                            onChange={e => this.setState({ selectedRoleId: Number(e.target.value) })}
                        >
                            <option value={0}>-- Select Role --</option>
                            {availableRoles.map(role => (
                                <option key={role.id} value={role.id}>
                                    {role.displayName || role.name}
                                </option>
                            ))}
                        </select>
                        <button
                            className="btn btn-sm btn-primary"
                            onClick={this.handleAssign}
                            disabled={selectedRoleId === 0}
                        >
                            Assign
                        </button>
                    </div>
                </td>
            </tr>
        );
    }
}
const ObservedUserRow = observer(UserRow);

// --- Main user list page ---
type UserListProps = Record<string, never>;

class UserList extends React.Component<UserListProps> {
    render() {
        return (
            <div className="container mt-4">
                <h3 className="mb-3">Users</h3>
                <table className="table table-bordered table-hover">
                    <thead className="table-light">
                        <tr>
                            <th>Name</th>
                            <th>Email</th>
                            <th>Current Roles</th>
                            <th>Assign Role</th>
                        </tr>
                    </thead>
                    <tbody>
                        {store.users.map(user => (
                            <ObservedUserRow key={user.id} store={store} user={user} />
                        ))}
                        {store.users.length === 0 && (
                            <tr>
                                <td colSpan={4} className="text-center text-muted">No users found.</td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </div>
        );
    }
}

const ObservedUserList = observer(UserList);
export default ObservedUserList;
