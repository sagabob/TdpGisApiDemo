import { useContext } from "react";
import SearchContext from "@/contexts/SearchContext";
import SimpleSearchComponent from "@/components/layouts/SimpleSearchComponent";
import { DropdownComponent } from "@/components/layouts/DropdownComponent";

const SearchBar = () => {
    const { searchValue, setSearchValue } = useContext(SearchContext);

    const className = "min-w-0 block w-full px-4 py-3 text-base font-normal text-gray-700 bg-white bg-clip-padding border border-solid border-gray-300 rounded shadow-md transition ease-in-out m-0 focus:text-gray-700 focus:bg-white focus:border-blue-600 focus:outline-none"

    const searchInput = { searchValue: searchValue, setSearchValue: setSearchValue, debounceTimeout: 300, cssClassName: className, cssStyle: null }
    return (
        <>
            <div className="fixed top-4 left-[10px] md:left-[40px] z-40 flex justify-center w-[calc(100%-20px)] md:w-auto">
                <div className="xl:w-[400px] w-full relative">
                    <SimpleSearchComponent {...searchInput} />
                    <DropdownComponent />
                </div>
            </div>
        </>);
}

export default SearchBar;