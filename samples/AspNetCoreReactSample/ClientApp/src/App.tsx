import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { ServiceContainer } from 'react-service-container';
import { UserManager } from './api';
import { Home, Layout, Login } from './components';

export default function App(): JSX.Element {
  const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
  return (
    <BrowserRouter basename={baseUrl || ''}>
      <ServiceContainer providers={[UserManager]}>
        <Layout>
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/login" element={<Login />} />
            <Route path="*" element={<Navigate to="/" />} />
          </Routes>
        </Layout>
      </ServiceContainer>
    </BrowserRouter>
  );
}
