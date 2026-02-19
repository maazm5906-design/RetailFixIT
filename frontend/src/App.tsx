import AppRoutes from './router';
import { useSignalR } from './hooks/useSignalR';

export default function App() {
  useSignalR();
  return <AppRoutes />;
}
