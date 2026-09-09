import { useState, useEffect } from 'react';
import apiService from '../services/apiService';
import './Dashboard.css';

export default function Dashboard() {
  const [stats, setStats] = useState({
    totalLeads: 0,
    totalPrograms: 0,
    totalTenants: 0,
    loading: true,
  });

  useEffect(() => {
    const fetchStats = async () => {
      try {
        setStats(prev => ({ ...prev, loading: true }));
        const [leads, programs, tenants] = await Promise.all([
          apiService.getLeads().catch(() => []),
          apiService.getPrograms().catch(() => []),
          apiService.getTenants().catch(() => []),
        ]);

        setStats({
          totalLeads: Array.isArray(leads) ? leads.length : 0,
          totalPrograms: Array.isArray(programs) ? programs.length : 0,
          totalTenants: Array.isArray(tenants) ? tenants.length : 0,
          loading: false,
        });
      } catch (error) {
        console.error('Failed to fetch stats:', error);
        setStats(prev => ({ ...prev, loading: false }));
      }
    };

    fetchStats();
  }, []);

  return (
    <div className="dashboard">
      <h1>Admin Dashboard</h1>
      <p className="dashboard-subtitle">Welcome to the Patient Care Management System</p>

      {stats.loading && <div className="alert alert-info">Loading statistics...</div>}

      <div className="stats-grid">
        <div className="stat-card stat-card-leads">
          <div className="stat-icon">👥</div>
          <div className="stat-content">
            <div className="stat-number">{stats.totalLeads}</div>
            <div className="stat-label">Total Leads</div>
          </div>
        </div>

        <div className="stat-card stat-card-programs">
          <div className="stat-icon">📋</div>
          <div className="stat-content">
            <div className="stat-number">{stats.totalPrograms}</div>
            <div className="stat-label">Programs</div>
          </div>
        </div>

        <div className="stat-card stat-card-tenants">
          <div className="stat-icon">🏢</div>
          <div className="stat-content">
            <div className="stat-number">{stats.totalTenants}</div>
            <div className="stat-label">Tenants</div>
          </div>
        </div>
      </div>

      <div className="dashboard-content">
        <section className="info-section">
          <h2>Quick Start Guide</h2>
          <ul>
            <li>
              <strong>Leads:</strong> Manage potential patients and prospects
            </li>
            <li>
              <strong>Programs:</strong> View and manage care programs
            </li>
            <li>
              <strong>Tenants:</strong> Manage system tenants and configurations
            </li>
          </ul>
        </section>
      </div>
    </div>
  );
}
