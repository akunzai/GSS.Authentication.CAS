import { useCallback, useEffect, useMemo, useState } from 'react';
import { NavLink } from 'react-router-dom';
import { UserManager } from '../api';
import { User } from '../types';

export function Home(): JSX.Element {
  const userManager = useMemo(() => new UserManager(), []);
  const [authenticated, setAuthenticated] = useState(false);
  const [user, setUser] = useState<User | null>(null);

  const fetchUser = useCallback(async () => {
    setUser(await userManager.getUser());
    setAuthenticated(await userManager.isAuthenticated());
  }, [userManager]);

  useEffect(() => {
    fetchUser();
  }, [fetchUser]);

  if (!authenticated) {
    return (
      <div>
        <h1>Hello anonymous</h1>
        <NavLink className="btn btn-primary" to="/login">
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
