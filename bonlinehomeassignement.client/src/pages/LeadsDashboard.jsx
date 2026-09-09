import { useState, useEffect } from 'react';
import apiService from '../services/apiService';
import './EntityDashboard.css';

export default function LeadsDashboard() {
  const [leads, setLeads] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    phone: '',
    source: '',
  });

  useEffect(() => {
    fetchLeads();
  }, []);

  const fetchLeads = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await apiService.getLeads();
      setLeads(Array.isArray(data) ? data : []);
    } catch (err) {
      setError('Failed to load leads. Using mock data.');
      // Use mock data on error with proper GUID format
      setLeads([
        { id: '550e8400-e29b-41d4-a716-446655440001', name: 'John Prospect', email: 'john@example.com', phone: '555-0001', source: 'Website' },
        { id: '550e8400-e29b-41d4-a716-446655440002', name: 'Jane Prospect', email: 'jane@example.com', phone: '555-0002', source: 'Referral' },
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
        await apiService.updateLead(editingId, formData);
        setLeads(leads.map(lead => lead.id === editingId ? { ...formData, id: editingId } : lead));
      } else {
        const newLead = await apiService.createLead(formData);
        setLeads([...leads, { ...formData, id: newLead.id || Date.now().toString() }]);
      }
      resetForm();
    } catch (err) {
      alert('Failed to save lead: ' + err.message);
    }
  };

  const handleEdit = (lead) => {
    setEditingId(lead.id);
    setFormData({
      name: lead.name || '',
      email: lead.email || '',
      phone: lead.phone || '',
      source: lead.source || '',
    });
    setShowForm(true);
  };

  const handleDelete = async (id) => {
    if (!window.confirm('Are you sure you want to delete this lead?')) return;
    try {
      await apiService.deleteLead(id);
      setLeads(leads.filter(lead => lead.id !== id));
    } catch (err) {
      alert('Failed to delete lead: ' + err.message);
    }
  };

  const resetForm = () => {
    setShowForm(false);
    setEditingId(null);
    setFormData({ name: '', email: '', phone: '', source: '' });
  };

  return (
    <div className="entity-dashboard">
      <div className="header">
        <h1>👥 Leads Management</h1>
        <button className="btn btn-primary" onClick={() => setShowForm(true)}>
          + Add New Lead
        </button>
      </div>

      {error && <div className="alert alert-warning">{error}</div>}

      {showForm && (
        <div className="form-container">
          <h2>{editingId ? 'Edit Lead' : 'New Lead'}</h2>
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label>Name *</label>
              <input
                type="text"
                name="name"
                value={formData.name}
                onChange={handleInputChange}
                required
                placeholder="Enter lead name"
              />
            </div>
            <div className="form-group">
              <label>Email *</label>
              <input
                type="email"
                name="email"
                value={formData.email}
                onChange={handleInputChange}
                required
                placeholder="Enter email address"
              />
            </div>
            <div className="form-group">
              <label>Phone</label>
              <input
                type="tel"
                name="phone"
                value={formData.phone}
                onChange={handleInputChange}
                placeholder="Enter phone number"
              />
            </div>
            <div className="form-group">
              <label>Source</label>
              <select name="source" value={formData.source} onChange={handleInputChange}>
                <option value="">Select a source</option>
                <option value="Website">Website</option>
                <option value="Referral">Referral</option>
                <option value="Phone">Phone Call</option>
                <option value="Email">Email</option>
                <option value="Other">Other</option>
              </select>
            </div>
            <div className="form-actions">
              <button type="submit" className="btn btn-success">
                {editingId ? 'Update Lead' : 'Create Lead'}
              </button>
              <button type="button" className="btn btn-secondary" onClick={resetForm}>
                Cancel
              </button>
            </div>
          </form>
        </div>
      )}

      {loading ? (
        <div className="alert alert-info">Loading leads...</div>
      ) : leads.length === 0 ? (
        <div className="alert alert-info">No leads found. Create one to get started!</div>
      ) : (
        <div className="table-container">
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Phone</th>
                <th>Source</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {leads.map(lead => (
                <tr key={lead.id}>
                  <td>{lead.name}</td>
                  <td>{lead.email}</td>
                  <td>{lead.phone || '-'}</td>
                  <td>{lead.source || '-'}</td>
                  <td className="actions">
                    <button
                      className="btn btn-sm btn-info"
                      onClick={() => handleEdit(lead)}
                    >
                      Edit
                    </button>
                    <button
                      className="btn btn-sm btn-danger"
                      onClick={() => handleDelete(lead.id)}
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
    </div>
  );
}
