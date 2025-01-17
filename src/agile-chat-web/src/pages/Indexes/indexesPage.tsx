import SimpleHeading from '@/components/Heading-Simple';
import IndexerComponent from '@/components/IndexerComponent';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { useNavigate } from 'react-router-dom';
import { useRoleContext } from "@/common/RoleContext";

export default function IndexesPage() {
  const navigate = useNavigate();
  const { isSystemAdmin } = useRoleContext();

  const handleNewContainer = () => {
    navigate('/container-form');
  };

  return (
    <div className="flex h-screen bg-background text-foreground">
      <div className="flex-1 flex flex-col">
        <SimpleHeading Title="Containers" Subtitle={'Configure your Containers'} DocumentCount={0} />
        <div className="flex flex-col h-full grow min-h-0 overflow-auto">
          <main className="flex-1 space-y-6">
            <Card>
            {isSystemAdmin && (
              <Button className="mb-6 h-11 px-4 ml-6 mt-6" onClick={handleNewContainer}>
                New Container
              </Button>
            )}
              <IndexerComponent />
            </Card>
          </main>
        </div>
      </div>
    </div>
  );
}
