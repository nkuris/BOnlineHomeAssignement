import Navigation from './Navigation';
import './Layout.css';

export default function Layout({ children }) {
  return (
    <div className="layout">
      <Navigation />
      <main className="layout-main">
        <div className="container-lg">
          {children}
        </div>
      </main>
    </div>
  );
}
