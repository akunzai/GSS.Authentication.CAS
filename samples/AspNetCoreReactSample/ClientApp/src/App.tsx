import { Redirect, Route, Router, Switch } from 'wouter';
import { Home, Layout, Login } from './components';

export default function App(): JSX.Element {
  const baseUrl = document
    .getElementsByTagName('base')[0]
    .getAttribute('href')
    ?.replace(/[/]$/, '');
  return (
    <Router base={baseUrl || ''}>
      <Layout>
        <Switch>
          <Route path="/">
            <Home />
          </Route>
          <Route path="/login">
            <Login />
          </Route>
          <Route>
            <Redirect to="/" />
          </Route>
        </Switch>
      </Layout>
    </Router>
  );
}
