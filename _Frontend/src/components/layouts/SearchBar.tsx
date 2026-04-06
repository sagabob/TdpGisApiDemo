import { useContext } from "react";
import SearchContext from "@/contexts/SearchContext";
import SimpleSearchComponent from "@/components/layouts/SimpleSearchComponent";
import { DropdownComponent } from "@/components/layouts/DropdownComponent";
import { WorkspaceEntityFilters } from "@/components/layouts/WorkspaceEntityFilters";

const SearchBar = () => {
    const { searchValue, setSearchValue } = useContext(SearchContext);

    const className =
        "min-w-0 block w-full px-3 py-2.5 text-sm font-normal text-gray-700 bg-white bg-clip-padding border border-solid border-gray-300 rounded-md shadow-sm transition ease-in-out m-0 focus:text-gray-700 focus:bg-white focus:border-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-500/20";

    const searchInput = { searchValue: searchValue, setSearchValue: setSearchValue, debounceTimeout: 300, cssClassName: className, cssStyle: null }
    return (
        <>
            <div className="fixed top-4 left-3 right-3 z-40 sm:left-6 sm:right-6 md:left-10 md:right-10">
                <div className="w-full max-w-[560px]">
                    <div className="flex flex-row items-start gap-2">
                        <div className="relative min-w-0 flex-1">
                            <SimpleSearchComponent {...searchInput} />
                            <DropdownComponent />
                        </div>
                        <WorkspaceEntityFilters />
                    </div>
                </div>
            </div>
        </>);
}

export default SearchBar;
