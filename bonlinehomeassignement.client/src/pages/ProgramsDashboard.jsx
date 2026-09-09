import { useState, useEffect } from 'react';
import apiService from '../services/apiService';
import './EntityDashboard.css';

export default function ProgramsDashboard() {
  const [programs, setPrograms] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    startDate: '',
    isActive: true,
  });

  useEffect(() => {
    fetchPrograms();
  }, []);

  const fetchPrograms = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await apiService.getPrograms();
      setPrograms(Array.isArray(data) ? data : []);
    } catch (err) {
      setError('Failed to load programs. Using mock data.');
      // Use mock data on error with proper GUID format
      setPrograms([
        {
          id: '550e8400-e29b-41d4-a716-446655440001',
          name: 'Basic Care Program',
          description: 'Entry-level care program for new patients',
          startDate: new Date().toISOString().split('T')[0],
          isActive: true,
        },
        {
          id: '550e8400-e29b-41d4-a716-446655440002',
          name: 'Advanced Care Program',
          description: 'Comprehensive care for complex cases',
          startDate: new Date().toISOString().split('T')[0],
          isActive: true,
        },
      ]);
    } finally {
      setLoading(false);
    }
  };

  const handleInputChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      if (editingId) {
        await apiService.updateProgram(editingId, formData);
        setPrograms(programs.map(program => program.id === editingId ? { ...formData, id: editingId } : program));
      } else {
        const newProgram = await apiService.createProgram(formData);
        setPrograms([...programs, { ...formData, id: newProgram.id || Date.now().toString() }]);
      }
      resetForm();
    } catch (err) {
      alert('Failed to save program: ' + err.message);
    }
  };

  const handleEdit = (program) => {
    setEditingId(program.id);
    setFormData({
      name: program.name || '',
      description: program.description || '',
      startDate: program.startDate ? program.startDate.split('T')[0] : '',
      isActive: program.isActive ?? true,
    });
    setShowForm(true);
  };

  const handleDelete = async (id) => {
    if (!window.confirm('Are you sure you want to delete this program?')) return;
    try {
      await apiService.deleteProgram(id);
      setPrograms(programs.filter(program => program.id !== id));
    } catch (err) {
      alert('Failed to delete program: ' + err.message);
    }
  };

  const resetForm = () => {
    setShowForm(false);
    setEditingId(null);
    setFormData({ name: '', description: '', startDate: '', isActive: true });
  };

  return (
    <div className="entity-dashboard">
      <div className="header">
        <h1>📋 Programs Management</h1>
        <button className="btn btn-primary" onClick={() => setShowForm(true)}>
          + Add New Program
        </button>
      </div>

      {error && <div className="alert alert-warning">{error}</div>}

      {showForm && (
        <div className="form-container">
          <h2>{editingId ? 'Edit Program' : 'New Program'}</h2>
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label>Program Name *</label>
              <input
                type="text"
                name="name"
                value={formData.name}
                onChange={handleInputChange}
                required
                placeholder="Enter program name"
              />
            </div>
            <div className="form-group">
              <label>Description</label>
              <textarea
                name="description"
                value={formData.description}
                onChange={handleInputChange}
                placeholder="Enter program description"
              ></textarea>
            </div>
            <div className="form-group">
              <label>Start Date</label>
              <input
                type="date"
                name="startDate"
                value={formData.startDate}
                onChange={handleInputChange}
              />
            </div>
            <div className="form-group">
              <label>
                <input
                  type="checkbox"
                  name="isActive"
                  checked={formData.isActive}
                  onChange={handleInputChange}
                  style={{ marginRight: '0.5rem' }}
                />
                Active Program
              </label>
            </div>
            <div className="form-actions">
              <button type="submit" className="btn btn-success">
                {editingId ? 'Update Program' : 'Create Program'}
              </button>
              <button type="button" className="btn btn-secondary" onClick={resetForm}>
                Cancel
              </button>
            </div>
          </form>
        </div>
      )}

      {loading ? (
        <div className="alert alert-info">Loading programs...</div>
      ) : programs.length === 0 ? (
        <div className="alert alert-info">No programs found. Create one to get started!</div>
      ) : (
        <div className="table-container">
          <table className="table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Description</th>
                <th>Start Date</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {programs.map(program => (
                <tr key={program.id}>
                  <td>{program.name}</td>
                  <td>{program.description || '-'}</td>
                  <td>{program.startDate ? new Date(program.startDate).toLocaleDateString() : '-'}</td>
                  <td>
                    <span className={`badge ${program.isActive ? 'badge-success' : 'badge-secondary'}`}>
                      {program.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td className="actions">
                    <button
                      className="btn btn-sm btn-info"
                      onClick={() => handleEdit(program)}
                    >
                      Edit
                    </button>
                    <button
                      className="btn btn-sm btn-danger"
                      onClick={() => handleDelete(program.id)}
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

const style = document.createElement('style');
style.textContent = `
  .badge {
    display: inline-block;
    padding: 0.35rem 0.65rem;
    border-radius: 12px;
    font-size: 0.85rem;
    font-weight: 500;
  }

  .badge-success {
    background-color: #d1e7dd;
    color: #0f5132;
  }

  .badge-secondary {
    background-color: #e2e3e5;
    color: #41464b;
  }
`;
if (!document.head.querySelector('style[data-badge-styles]')) {
  style.setAttribute('data-badge-styles', 'true');
  document.head.appendChild(style);
}
