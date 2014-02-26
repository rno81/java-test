/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */

package samplejava.print;

/**
 *
 * @author promobile
 */
public class Println {

    public static final void print(String s){
        System.out.println(s);
    }


    public static final void print(int i){
        System.out.println(i);
    }
	
	public static final void print(long i){
        System.out.println(i);
    }
	
	public static final void print(double d){
        System.out.println(d);
    }
	
	public static final void print(char character){
        System.out.println(character);
    }

	//zmiana PPL
	
	public static final void print(char character, boolean silent){
		if(!silent){
			System.out.println(character);
		}
    }


}
