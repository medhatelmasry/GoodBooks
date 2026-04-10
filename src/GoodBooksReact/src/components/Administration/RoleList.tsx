import * as React from 'react';
import { observer } from 'mobx-react';
import AdministrationStore, { Role, Permission } from '../Shared/Stores/Administration/AdministrationStore';

const store = new AdministrationStore();

// --- Permission checkbox row inside modal ---
type PermissionCheckProps = {
    store: AdministrationStore;
    roleId: number;
    permission: Permission;
    checked: boolean;
};

class PermissionCheck extends React.Component<PermissionCheckProps> {
    handleChange = () => {
        const { store, roleId, permission, checked } = this.props;
        store.togglePermission(roleId, permission.id, checked);
    };

    render() {
        const { permission, checked } = this.props;
        return (
            <div className="form-check">
                <input
                    className="form-check-input"
                    type="checkbox"
                    id={'perm-' + permission.id}
                    checked={checked}
                    onChange={this.handleChange}
                />
                <label className="form-check-label" htmlFor={'perm-' + permission.id}>
                    {permission.displayName || permission.name}
                </label>
            </div>
        );
    }
}
const ObservedPermissionCheck = observer(PermissionCheck);

// --- Role create/edit modal ---
type RoleModalProps = {
    store: AdministrationStore;
};

class RoleModal extends React.Component<RoleModalProps> {
    handleSave = () => {
        this.props.store.saveRole();
    };

    handleCancel = () => {
        this.props.store.closeRoleModal();
    };

    getCheckedPermissionIds(): Set<number> {
        const { selectedRole } = this.props.store;
        if (!selectedRole) return new Set();
        return new Set(selectedRole.permissions.map(p => p.id));
    }

    render() {
        const { store } = this.props;
        if (!store.showRoleModal) return null;

        const checkedIds = this.getCheckedPermissionIds();
        const isEdit = store.selectedRole !== null;

        return (
            <div
                className="modal d-block"
                tabIndex={-1}
                style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}
            >
                <div className="modal-dialog modal-lg">
                    <div className="modal-content">
                        <div className="modal-header">
                            <h5 className="modal-title">{isEdit ? 'Edit Role' : 'Create New Role'}</h5>
                            <button type="button" className="btn-close" onClick={this.handleCancel} />
                        </div>
                        <div className="modal-body">
                            {store.validationErrors.length > 0 && (
                                <div className="alert alert-danger">
                                    {store.validationErrors.map((err, i) => <div key={i}>{err}</div>)}
                                </div>
                            )}
                            <div className="mb-3">
                                <label className="form-label">Role Name</label>
                                <input
                                    type="text"
                                    className="form-control"
                                    value={store.formName}
                                    onChange={e => { store.formName = e.target.value; }}
                                />
                            </div>
                            <div className="mb-3">
                                <label className="form-label">Display Name</label>
                                <input
                                    type="text"
                                    className="form-control"
                                    value={store.formDisplayName}
                                    onChange={e => { store.formDisplayName = e.target.value; }}
                                />
                            </div>
                            {isEdit && store.groups.length > 0 && (
                                <div className="mb-3">
                                    <label className="form-label">Permissions</label>
                                    {store.groups.map(group => (
                                        <div key={group.id} className="mb-2">
                                            <strong>{group.displayName || group.name}</strong>
                                            <div className="ms-3">
                                                {group.permissions.map(perm => (
                                                    <ObservedPermissionCheck
                                                        key={perm.id}
                                                        store={store}
                                                        roleId={store.selectedRole!.id}
                                                        permission={perm}
                                                        checked={checkedIds.has(perm.id)}
                                                    />
                                                ))}
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                        <div className="modal-footer">
                            <button type="button" className="btn btn-secondary" onClick={this.handleCancel}>
                                Cancel
                            </button>
                            <button type="button" className="btn btn-primary" onClick={this.handleSave}>
                                Save
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        );
    }
}
const ObservedRoleModal = observer(RoleModal);

// --- Single role row in the table ---
type RoleRowProps = {
    store: AdministrationStore;
    role: Role;
};

class RoleRow extends React.Component<RoleRowProps> {
    handleEdit = () => {
        this.props.store.openEditRoleModal(this.props.role);
    };

    handleDelete = () => {
        const { role, store } = this.props;
        if (confirm('Are you sure you want to delete role "' + (role.displayName || role.name) + '"?')) {
            store.deleteRole(role.id);
        }
    };

    render() {
        const { role } = this.props;
        const isProtected = role.sysAdmin || role.system;

        return (
            <tr>
                <td>{role.name}</td>
                <td>{role.displayName}</td>
                <td>{role.permissions ? role.permissions.length : 0}</td>
                <td>
                    <button
                        className="btn btn-sm btn-outline-primary me-2"
                        onClick={this.handleEdit}
                        disabled={isProtected}
                        title={isProtected ? 'System roles cannot be edited' : 'Edit role'}
                    >
                        Edit
                    </button>
                    <button
                        className="btn btn-sm btn-outline-danger"
                        onClick={this.handleDelete}
                        disabled={isProtected}
                        title={isProtected ? 'System roles cannot be deleted' : 'Delete role'}
                    >
                        Delete
                    </button>
                </td>
            </tr>
        );
    }
}
const ObservedRoleRow = observer(RoleRow);

// --- Main role list page ---
type RoleListProps = Record<string, never>;

class RoleList extends React.Component<RoleListProps> {
    handleCreate = () => {
        store.openCreateRoleModal();
    };

    render() {
        return (
            <div className="container mt-4">
                <ObservedRoleModal store={store} />
                <div className="d-flex justify-content-between align-items-center mb-3">
                    <h3>Roles</h3>
                    <button className="btn btn-success" onClick={this.handleCreate}>
                        + Create New Role
                    </button>
                </div>
                <table className="table table-bordered table-hover">
                    <thead className="table-light">
                        <tr>
                            <th>Name</th>
                            <th>Display Name</th>
                            <th>Permissions</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {store.roles.map(role => (
                            <ObservedRoleRow key={role.id} store={store} role={role} />
                        ))}
                        {store.roles.length === 0 && (
                            <tr>
                                <td colSpan={4} className="text-center text-muted">No roles found.</td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </div>
        );
    }
}

const ObservedRoleList = observer(RoleList);
export default ObservedRoleList;
