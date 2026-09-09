import { useState, useEffect } from 'react';
import apiService from '../services/apiService';
import './EntityDashboard.css';

export default function TenantsDashboard() {
  const [tenants, setTenants] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [formData, setFormData] = useState({
    name: '',
    hostname: '',
  });

  useEffect(() => {
    fetchTenants();
  }, []);

  const fetchTenants = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await apiService.getTenants();
      setTenants(Array.isArray(data) ? data : []);
    } catch (err) {
      setError('Failed to load tenants. Using mock data.');
      // Use mock data on error with proper GUID format
      setTenants([
        { id: '550e8400-e29b-41d4-a716-446655440001', name: 'Default Tenant', hostname: 'localhost' },
        { id: '550e8400-e29b-41d4-a716-446655440002', name: 'Healthcare Provider Inc', hostname: 'acme.bhome.local' },
      ]);
    } finally {
      setLoading(false);
    }
  };

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (editingId) {
        await apiService.updateTenant(editingId, formData);
        setTenants(tenants.map(tenant => tenant.id === editingId ? { ...formData, id: editingId } : tenant));
      } else {
        const newTenant = await apiService.createTenant(formData);
        setTenants([...tenants, { ...formData, id: newTenant.id || Date.now().toString() }]);
      }
      resetForm();
    } catch (err) {
      alert('Failed to save tenant: ' + err.message);
    }
  };

  const handleEdit = (tenant) => {
    setEditingId(tenant.id);
    setFormData({
      name: tenant.name || '',
      hostname: tenant.hostname || '',
    });
    setShowForm(true);
  };

  const handleDelete = async (id) => {
    if (!window.confirm('Are you sure you want to delete this tenant? This action cannot be undone.')) return;
    try {
      await apiService.deleteTenant(id);
      setTenants(tenants.filter(tenant => tenant.id !== id));
    } catch (err) {
      alert('Failed to delete tenant: ' + err.message);
    }
  };

  const resetForm = () => {
    setShowForm(false);
    setEditingId(null);
    setFormData({ name: '', hostname: '' });
  };

  return (
    <div className="entity-dashboard">
      <div className="header">
        <h1>🏢 Tenants Management</h1>
        <button className="btn btn-primary" onClick={() => setShowForm(true)}>
          + Add New Tenant
        </button>
      </div>

      {error && <div className="alert alert-warning">{error}</div>}

      {showForm && (
        <div className="form-container">
          <h2>{editingId ? 'Edit Tenant' : 'New Tenant'}</h2>
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label>Tenant Name *</label>
              <input
                type="text"
                name="name"
                value={formData.name}
                onChange={handleInputChange}
                required
                placeholder="Enter tenant organization name"
              />
            </div>
            <div className="form-group">
              <label>Hostname *</label>
              <input
                type="text"
                name="hostname"
                value={formData.hostname}
                onChange={handleInputChange}
                required
                placeholder="e.g., tenant.bhome.local or localhost"
              />
              <small style={{ color: '#6c757d', marginTop: '0.25rem', display: 'block' }}>
                The hostname where this tenant's subdomain is hosted
              </small>
            </div>
            <div className="form-actions">
              <button type="submit" className="btn btn-success">
                {editingId ? 'Update Tenant' : 'Create Tenant'}
              </button>
              <button type="button" className="btn btn-secondary" onClick={resetForm}>
                Cancel
              </button>
            </div>
          </form>
        </div>
      )}

      {loading ? (
        <div className="alert alert-info">Loading tenants...</div>
      ) : tenants.length === 0 ? (
        <div className="alert alert-info">No tenants found. Create one to get started!</div>
      ) : (
        <div className="table-container">
          <table className="table">
            <thead>
              <tr>
                <th>Tenant Name</th>
                <th>Hostname</th>
                <th>Created</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {tenants.map(tenant => (
                <tr key={tenant.id}>
                  <td>
                    <strong>{tenant.name}</strong>
                  </td>
                  <td>
                    <code style={{ backgroundColor: '#f8f9fa', padding: '0.25rem 0.5rem', borderRadius: '3px' }}>
                      {tenant.hostname}
                    </code>
                  </td>
                  <td>{tenant.createdAt ? new Date(tenant.createdAt).toLocaleDateString() : '-'}</td>
                  <td className="actions">
                    <button
                      className="btn btn-sm btn-info"
                      onClick={() => handleEdit(tenant)}
                    >
                      Edit
                    </button>
                    <button
                      className="btn btn-sm btn-danger"
                      onClick={() => handleDelete(tenant.id)}
                    >
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div style={{ marginTop: '2rem', padding: '1.5rem', backgroundColor: '#e7f3ff', borderLeft: '4px solid #0d6efd', borderRadius: '4px' }}>
        <h3 style={{ color: '#0d6efd', marginTop: 0 }}>📌 About Tenants</h3>
        <p style={{ color: '#495057', marginBottom: 0 }}>
          Tenants represent separate customer organizations in the multi-tenant system. Each tenant has isolated data and can be accessed through different hostnames. Configure hostnames in your DNS and application configuration to route requests appropriately.
        </p>
      </div>
    </div>
  );
}
