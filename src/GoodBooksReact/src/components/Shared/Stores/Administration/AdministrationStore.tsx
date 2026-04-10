import { makeObservable, observable } from 'mobx';
import axios from 'axios';
import Config from '../../Config';

export interface Permission {
    id: number;
    name: string;
    displayName: string;
}

export interface Group {
    id: number;
    name: string;
    displayName: string;
    permissions: Permission[];
}

export interface Role {
    id: number;
    name: string;
    displayName: string;
    sysAdmin: boolean;
    system: boolean;
    permissions: Permission[];
}

export interface User {
    id: number;
    firstName: string;
    lastName: string;
    userName: string;
    email: string;
    roles: Role[];
}

export default class AdministrationStore {
    roles: Role[] = [];
    users: User[] = [];
    groups: Group[] = [];
    validationErrors: string[] = [];
    isLoading = false;
    showRoleModal = false;
    selectedRole: Role | null = null;

    // Form state for the modal
    formName = '';
    formDisplayName = '';

    constructor() {
        makeObservable(this, {
            roles: observable,
            users: observable,
            groups: observable,
            validationErrors: observable,
            isLoading: observable,
            showRoleModal: observable,
            selectedRole: observable,
            formName: observable,
            formDisplayName: observable,
        });

        this.loadRoles();
        this.loadUsers();
        this.loadGroups();
    }

    loadRoles() {
        axios.get(Config.API_URL + 'Administration/roles')
            .then(result => {
                this.roles = result.data;
            })
            .catch(error => {
                console.error('Failed to load roles:', error);
            });
    }

    loadUsers() {
        axios.get(Config.API_URL + 'Administration/users')
            .then(result => {
                this.users = result.data;
            })
            .catch(error => {
                console.error('Failed to load users:', error);
            });
    }

    loadGroups() {
        axios.get(Config.API_URL + 'Administration/groups')
            .then(result => {
                this.groups = result.data;
            })
            .catch(error => {
                console.error('Failed to load groups:', error);
            });
    }

    openCreateRoleModal() {
        this.selectedRole = null;
        this.formName = '';
        this.formDisplayName = '';
        this.validationErrors = [];
        this.showRoleModal = true;
    }

    openEditRoleModal(role: Role) {
        this.selectedRole = role;
        this.formName = role.name;
        this.formDisplayName = role.displayName;
        this.validationErrors = [];
        this.showRoleModal = true;
    }

    closeRoleModal() {
        this.showRoleModal = false;
        this.validationErrors = [];
    }

    saveRole() {
        const payload = {
            id: this.selectedRole ? this.selectedRole.id : 0,
            name: this.formName,
            displayName: this.formDisplayName,
        };

        axios.post(Config.API_URL + 'Administration/SaveRole', JSON.stringify(payload), {
            headers: { 'Content-type': 'application/json' }
        })
            .then(() => {
                this.showRoleModal = false;
                this.loadRoles();
            })
            .catch(error => {
                if (error.response && error.response.data) {
                    this.validationErrors = Array.isArray(error.response.data)
                        ? error.response.data
                        : [error.response.data];
                } else {
                    this.validationErrors = ['An error occurred while saving the role.'];
                }
            });
    }

    deleteRole(roleId: number) {
        axios.delete(Config.API_URL + 'Administration/DeleteRole/' + roleId)
            .then(() => {
                this.loadRoles();
            })
            .catch(error => {
                if (error.response && error.response.data) {
                    const msg = Array.isArray(error.response.data)
                        ? error.response.data[0]
                        : error.response.data;
                    alert(msg);
                } else {
                    alert('An error occurred while deleting the role.');
                }
            });
    }

    togglePermission(roleId: number, permissionId: number, currentlyChecked: boolean) {
        const endpoint = currentlyChecked
            ? 'Administration/RemovePermissionFromRole'
            : 'Administration/AddPermissionToRole';

        const payload = { roleId, permissionId };

        axios.post(Config.API_URL + endpoint, JSON.stringify(payload), {
            headers: { 'Content-type': 'application/json' }
        })
            .then(() => {
                this.loadRoles();
            })
            .catch(error => {
                console.error('Failed to toggle permission:', error);
            });
    }

    assignRoleToUser(userId: number, roleId: number) {
        const payload = { userId, roleId };

        axios.post(Config.API_URL + 'Administration/AssignRoleToUser', JSON.stringify(payload), {
            headers: { 'Content-type': 'application/json' }
        })
            .then(() => {
                this.loadUsers();
            })
            .catch(error => {
                if (error.response && error.response.data) {
                    const msg = Array.isArray(error.response.data)
                        ? error.response.data[0]
                        : error.response.data;
                    alert(msg);
                } else {
                    alert('An error occurred while assigning the role.');
                }
            });
    }

    removeRoleFromUser(userId: number, roleId: number) {
        const payload = { userId, roleId };

        axios.post(Config.API_URL + 'Administration/RemoveRoleFromUser', JSON.stringify(payload), {
            headers: { 'Content-type': 'application/json' }
        })
            .then(() => {
                this.loadUsers();
            })
            .catch(error => {
                if (error.response && error.response.data) {
                    const msg = Array.isArray(error.response.data)
                        ? error.response.data[0]
                        : error.response.data;
                    alert(msg);
                } else {
                    alert('An error occurred while removing the role.');
                }
            });
    }
}
