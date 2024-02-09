import { useState } from "react";
import { ObjectGroupInterface } from "../../interfaces/globalInterfaces";
import fetchObjectGroups from "../../services/objectGroupsService";

const useObjectGroups = (objectType: string) => {
  const [objectGroups, setObjectGroups] = useState<ObjectGroupInterface[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const fetchObjectGroupsData = async () => {
    const result = await fetchObjectGroups(objectType);

    setObjectGroups(result.objectGroups);
    setIsLoading(result.isLoading);
    setError(result.error);
  };

  return {
    objectGroups,
    isLoading,
    error,
    fetchObjectGroups: fetchObjectGroupsData,
  };
};

export default useObjectGroups;
