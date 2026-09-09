import { Link, useLocation } from 'react-router-dom';
import './Navigation.css';

export default function Navigation() {
  const location = useLocation();

  const isActive = (path) => location.pathname === path;

  return (
    <nav className="navbar navbar-dark bg-dark">
      <div className="container-fluid">
        <Link className="navbar-brand" to="/dashboard">
          <span className="navbar-icon">📊</span>
          Patient Care Admin
        </Link>
        <div className="navbar-nav flex-row">
          <Link
            className={`nav-link ${isActive('/dashboard') ? 'active' : ''}`}
            to="/dashboard"
          >
            Dashboard
          </Link>
          <Link
            className={`nav-link ${isActive('/leads') ? 'active' : ''}`}
            to="/leads"
          >
            Leads
          </Link>
          <Link
            className={`nav-link ${isActive('/programs') ? 'active' : ''}`}
            to="/programs"
          >
            Programs
          </Link>
          <Link
            className={`nav-link ${isActive('/tenants') ? 'active' : ''}`}
            to="/tenants"
          >
            Tenants
          </Link>
        </div>
      </div>
    </nav>
  );
}
