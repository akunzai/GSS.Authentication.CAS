import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { Home, Layout, Login } from './components';

export default function App(): JSX.Element {
  const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
  return (
    <BrowserRouter basename={baseUrl || ''}>
        <Layout>
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/login" element={<Login />} />
            <Route path="*" element={<Navigate to="/" />} />
          </Routes>
        </Layout>
    </BrowserRouter>
  );
}
