import { useCallback, useEffect, useState } from 'react';
import { NavLink } from 'react-router-dom';
import { useService } from 'react-service-container';
import { UserManager } from '../api';
import { User } from '../types';

export function Home(): JSX.Element {
  const userManager = useService(UserManager);
  const [authenticated, setAuthenticated] = useState(false);
  const [user, setUser] = useState<User | null>(null);

  const fetchUser = useCallback(async () => {
    setUser(await userManager.getUser(!authenticated));
    setAuthenticated(await userManager.isAuthenticated());
  }, [authenticated, userManager]);

  useEffect(() => {
    fetchUser();
  }, [fetchUser]);

  if (!authenticated) {
    return (
      <div>
        <h1>Hello anonymous</h1>
        <NavLink className="btn btn-primary" to="/Account/Login">
          Login
        </NavLink>
      </div>
    );
  }
  return (
    <div>
      <h1>Hello {user?.name}</h1>
      <dl>
        <dt>ID</dt>
        <dd>{user?.id}</dd>
      </dl>
      <dl>
        <dt>Email</dt>
        <dd>{user?.email}</dd>
      </dl>
      <button className="btn btn-danger" onClick={() => userManager.signOut()}>
        Logout
      </button>
    </div>
  );
}
